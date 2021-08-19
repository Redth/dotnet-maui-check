using DotNetCheck.DotNet;
using DotNetCheck.Models;
using Microsoft.NET.Sdk.WorkloadManifestReader;
using Newtonsoft.Json;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Packaging.Core;
using NuGet.Packaging.Signing;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace DotNetCheck
{
	public class AcquirePackagesCommand : AsyncCommand<AcquirePackagesSettings>
	{
		public override async Task<int> ExecuteAsync(CommandContext context, AcquirePackagesSettings settings)
		{
			AnsiConsole.Markup($"[bold blue]{Icon.Thinking} Synchronizing configuration...[/]");

			var manifest = await ToolInfo.LoadManifest(settings.Manifest, settings.GetManifestChannel());

			if (!ToolInfo.Validate(manifest))
			{
				ToolInfo.ExitPrompt(settings.NonInteractive);
				return -1;
			}

			AnsiConsole.MarkupLine(" ok");

			var manifestDotNetSdk = manifest?.Check?.DotNet?.Sdks?.FirstOrDefault();
			
			var dn = new DotNetSdk(new SharedState());
			var sdks = await dn.GetSdks();

			DotNetSdkInfo bestSdk = null;

			foreach (var sdk in sdks)
			{
				if (bestSdk == null || sdk.Version >= bestSdk.Version)
				{
					bestSdk = sdk;

					if (bestSdk.Version == NuGetVersion.Parse(manifestDotNetSdk.Version))
						break;
				}
			}

			var sdkRoot = dn.DotNetSdkLocation.FullName;
			var sdkVersion = bestSdk.Version.ToString();
			var cancelTokenSource = new CancellationTokenSource();

			var nugetWorkloadManifestProvider = new NuGetManifestProvider(new NuGetVersion(sdkVersion));

			await nugetWorkloadManifestProvider.AcquireManifests(settings.DownloadDirectory, manifestDotNetSdk.PackageSources, manifestDotNetSdk.Workloads, cancelTokenSource.Token);

			foreach (var workload in manifestDotNetSdk.Workloads)
			{
				AnsiConsole.MarkupLine($"Acquiring packages for: {workload.Id} ...");

				var items = new Dictionary<string, string>();

				foreach (var mfst in nugetWorkloadManifestProvider.GetManifests())
				{
					var manifestReader = WorkloadManifestReader.ReadWorkloadManifest(workload.WorkloadManifestId, mfst.openManifestStream());

					foreach (var wlPack in manifestReader.Packs)
					{
						if (wlPack.Value.IsAlias)
						{
							foreach (var wlPackAlias in wlPack.Value.AliasTo.Values.Distinct())
							{
								var packageId = wlPackAlias.ToString();
								var packageVersion = wlPack.Value.Version;

								await GetNuGetDependencyTree(settings.DownloadDirectory, manifestDotNetSdk.PackageSources, packageId, new NuGetVersion(packageVersion), cancelTokenSource.Token);
							}
						}
						else
						{
							var packageId = wlPack.Value.ToString();
							var packageVersion = wlPack.Value.Version;

							await GetNuGetDependencyTree(settings.DownloadDirectory, manifestDotNetSdk.PackageSources, packageId, new NuGetVersion(packageVersion), cancelTokenSource.Token);
						}
					}
				}
			}

			ToolInfo.ExitPrompt(settings.NonInteractive);
			return 0;
		}


		async Task GetNuGetDependencyTree(string destinationDir, IEnumerable<string> packageSources, string packageId, NuGetVersion packageVersion, CancellationToken cancelToken)
		{
			var cache = NullSourceCacheContext.Instance;
			var logger = NullLogger.Instance;

			foreach (var pkgSrc in packageSources)
			{
				var nugetSource = Repository.Factory.GetCoreV3(pkgSrc);
				var byIdRes = await nugetSource.GetResourceAsync<FindPackageByIdResource>();

				// Cause a retry if this is null
				if (byIdRes == null)
					throw new InvalidDataException();

				if (await DownloadPackage(destinationDir, nugetSource, cache, logger, byIdRes, packageId, new VersionRange(packageVersion), cancelToken))
					break;
			}
		}

		async Task<bool> DownloadPackage(string directory, SourceRepository nugetSource, SourceCacheContext cache, ILogger logger, FindPackageByIdResource byIdRes, string packageId, VersionRange versionRange, CancellationToken cancelToken)
		{
			var packageVersionsAvailable = await byIdRes.GetAllVersionsAsync(packageId, cache, logger, cancelToken);

			if (!(packageVersionsAvailable?.Any() ?? false))
				return false;

			var bestVersion = versionRange.FindBestMatch(packageVersionsAvailable);
			if (bestVersion == null)
				return false;

			async Task<bool> download(PackageIdentity pkgIdentity)
			{
				var destFile = Path.Combine(directory, $"{pkgIdentity.Id}.{pkgIdentity.Version}.nupkg");

				if (!File.Exists(destFile))
				{
					AnsiConsole.MarkupLine($"\t{pkgIdentity.Id} {pkgIdentity.Version} ...");

					using var downloader = await byIdRes.GetPackageDownloaderAsync(pkgIdentity, cache, logger, cancelToken);

					await downloader.CopyNupkgFileToAsync(destFile, cancelToken);
				}

				return true;
			}

			bool foundAll = false;

			if (await byIdRes.DoesPackageExistAsync(packageId, bestVersion, cache, logger, cancelToken))
			{
				foundAll = true;

				if (!await download(new PackageIdentity(packageId, bestVersion)))
					return false;

				var deps = await byIdRes.GetDependencyInfoAsync(packageId, bestVersion, cache, logger, cancelToken);

				foreach (var depGrp in deps.DependencyGroups)
				{
					foreach (var depPkg in depGrp.Packages)
					{
						if (!await DownloadPackage(directory, nugetSource, cache, logger, byIdRes, depPkg.Id, depPkg.VersionRange, cancelToken))
							foundAll = false;
					}
				}
			}

			return foundAll;
		}

	}

	class NuGetManifestProvider : IWorkloadManifestProvider
	{
		public NuGetManifestProvider(NuGetVersion sdkVersion)
		{
			SdkVersion = sdkVersion;
			ManifestPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
			Directory.CreateDirectory(ManifestPath);
		}

		public readonly NuGetVersion SdkVersion;
		public readonly string ManifestPath;

		List<(string id, string dir, string file)> manifestDirs = new();

		public async Task AcquireManifests(string directory, List<string> packageSources, List<Manifest.DotNetWorkload> workloads, CancellationToken cancelToken)
		{
			var cache = NullSourceCacheContext.Instance;
			var logger = NullLogger.Instance;

			foreach (var workload in workloads)
			{
				AnsiConsole.MarkupLine($"Acquiring Workload Manifest: {workload.PackageId} {workload.Version} ...");

				var manifestDirName = Regex.Replace(
						workload.PackageId,
						@"\.Manifest-\d+\.\d+\.\d+$",
						string.Empty,
						RegexOptions.Singleline | RegexOptions.IgnoreCase)
							?.ToLowerInvariant();

				var manifestDir = Path.Combine(ManifestPath, manifestDirName);

				Directory.CreateDirectory(manifestDir);

				foreach (var pkgSrc in packageSources)
				{
					var nugetSource = Repository.Factory.GetCoreV3(pkgSrc);
					var byIdRes = await nugetSource.GetResourceAsync<FindPackageByIdResource>();

					// Cause a retry if this is null
					if (byIdRes == null)
						throw new InvalidDataException();

					var pkgIdentity = new PackageIdentity(workload.PackageId, new NuGetVersion(workload.Version));

					using var downloader = await byIdRes.GetPackageDownloaderAsync(pkgIdentity, cache, logger, cancelToken);

					if (downloader == null)
						continue;

					var nupkgFile = Path.Combine(directory, $"{workload.PackageId}.{workload.Version}.nupkg");
					await downloader.CopyNupkgFileToAsync(nupkgFile, cancelToken);

					using (var sr = File.OpenRead(nupkgFile))
					using (var zipArchive = new System.IO.Compression.ZipArchive(sr, System.IO.Compression.ZipArchiveMode.Read))
					{
						foreach (var zipEntry in zipArchive.Entries)
						{
							var entryName = zipEntry.FullName;

							if (!entryName.EndsWith("WorkloadManifest.json", StringComparison.Ordinal))
								continue;

							var workloadFile = Path.Combine(manifestDir, "WorkloadManifest.json");
							using (var manifestZipStream = zipEntry.Open())
							using (var manifestFileStream = File.Create(workloadFile))
							{
								await manifestZipStream.CopyToAsync(manifestFileStream);
							}

							manifestDirs.Add((workload.Id, manifestDir, workloadFile));
						}
					}

					break;
				}
			}
		}

		public IEnumerable<string> GetManifestDirectories()
			=> manifestDirs.Select(m => m.dir);

		public IEnumerable<(string manifestId, string informationalPath, Func<Stream> openManifestStream)> GetManifests()
					=> manifestDirs.Select(m => (m.id, m.dir, new Func<Stream>(() => File.OpenRead(m.file))));

		public string GetSdkFeatureBand()
			=> $"{SdkVersion.Major}.{SdkVersion.Minor}.{SdkVersion.Patch}";
	}
}
