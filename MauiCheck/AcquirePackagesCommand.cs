using DotNetCheck.DotNet;
using DotNetCheck.Models;
using Microsoft.NET.Sdk.WorkloadManifestReader;
using Newtonsoft.Json;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Packaging;
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
		readonly static string[] WorkloadPackageMsiAliases = new[]
		{
			".Msi.x86",
			".Msi.x64",
			".Msi.arm64"
		};

		internal static Dictionary<string, (SourceRepository source, FindPackageByIdResource byIdRes)> nugetSources = new();

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

			foreach (var s in manifestDotNetSdk.PackageSources)
			{
				var nugetSource = Repository.Factory.GetCoreV3(s);
				var byIdRes = await nugetSource.GetResourceAsync<FindPackageByIdResource>();

				nugetSources.Add(s, (nugetSource, byIdRes));
			}

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

			// Make sure the download dir exists
			Directory.CreateDirectory(settings.DownloadDirectory);

			var sdkRoot = dn.DotNetSdkLocation.FullName;
			var sdkVersion = bestSdk.Version.ToString();
			var cancelTokenSource = new CancellationTokenSource();

			var nugetWorkloadManifestProvider = new NuGetManifestProvider(new NuGetVersion(sdkVersion));

			foreach (var workload in manifestDotNetSdk.Workloads)
			{
				AnsiConsole.MarkupLine($"Acquiring Workload Manifest: {workload.PackageId} {workload.Version} ...");

				await GetNuGetDependencyTree(settings.DownloadDirectory, workload.PackageId, new NuGetVersion(workload.Version), cancelTokenSource.Token, false);

				foreach (var msiAlias in WorkloadPackageMsiAliases)
				{
					var msiPackageId = $"{workload.PackageId}{msiAlias}";
					await GetNuGetDependencyTree(settings.DownloadDirectory, msiPackageId, new NuGetVersion(workload.Version), cancelTokenSource.Token, false);
				}
			}

			await nugetWorkloadManifestProvider.ParseManifestPackages(settings.DownloadDirectory, manifestDotNetSdk.Workloads, cancelTokenSource.Token);

			var items = new Dictionary<string, string>();

			try
			{
				var manifests = nugetWorkloadManifestProvider.GetManifests();
				foreach (var mfst in manifests)
				{
					AnsiConsole.MarkupLine($"Acquiring packages for: {mfst.manifestId} ...");

					var manifestReader = WorkloadManifestReader.ReadWorkloadManifest(mfst.manifestId, mfst.openManifestStream());

					foreach (var wlPack in manifestReader.Packs)
					{
						if (wlPack.Value.IsAlias)
						{
							foreach (var wlPackAlias in wlPack.Value.AliasTo.Values.Distinct())
							{
								var packageId = wlPackAlias.ToString();
								var packageVersion = wlPack.Value.Version;

								await GetNuGetDependencyTree(settings.DownloadDirectory, packageId, new NuGetVersion(packageVersion), cancelTokenSource.Token, true);

								foreach (var msiAlias in WorkloadPackageMsiAliases)
								{
									var msiPackageId = $"{packageId}{msiAlias}";
									await GetNuGetDependencyTree(settings.DownloadDirectory, msiPackageId, new NuGetVersion(packageVersion), cancelTokenSource.Token, true);
								}
							}
						}
						else
						{
							var packageId = wlPack.Value.Id;
							var packageVersion = wlPack.Value.Version;

							await GetNuGetDependencyTree(settings.DownloadDirectory, packageId, new NuGetVersion(packageVersion), cancelTokenSource.Token, true);

							foreach (var msiAlias in WorkloadPackageMsiAliases)
							{
								var msiPackageId = $"{packageId}{msiAlias}";
								await GetNuGetDependencyTree(settings.DownloadDirectory, msiPackageId, new NuGetVersion(packageVersion), cancelTokenSource.Token, true);
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				Util.Exception(ex);
			}

			ToolInfo.ExitPrompt(settings.NonInteractive);
			return 0;
		}


		async Task GetNuGetDependencyTree(string destinationDir, string packageId, NuGetVersion packageVersion, CancellationToken cancelToken, bool includeDependencies)
		{
			var cache = new SourceCacheContext();
			cache.DirectDownload = true;
			cache.NoCache = true;

			var logger = NullLogger.Instance;

			foreach (var src in nugetSources)
			{
				if (await DownloadPackage(destinationDir, src.Value.source, cache, logger, src.Value.byIdRes, packageId, packageVersion, cancelToken, includeDependencies))
					break;
			}
		}

		async Task<bool> DownloadPackage(string directory, SourceRepository nugetSource, SourceCacheContext cache, ILogger logger, FindPackageByIdResource byIdRes, string packageId, NuGetVersion packageVersion, CancellationToken cancelToken, bool includeDependencies)
		{
			var packageVersionsAvailable = await byIdRes.GetAllVersionsAsync(packageId, cache, logger, cancelToken);

			if (!(packageVersionsAvailable?.Any() ?? false))
				return false;

			// Require the exact version, otherwise we'll try other feeds
			var matchingExplicitVersion = packageVersionsAvailable.FirstOrDefault(pv => pv == packageVersion);
			if (matchingExplicitVersion == null)
				return false;

			async Task<PackageArchiveReader> download(string destFile, PackageIdentity pkgIdentity)
			{
				var tries = 0;

				while (tries <= 3)
				{
					tries++;

					try
					{
						if (!File.Exists(destFile) || tries > 1)
						{
							using var downloader = await byIdRes.GetPackageDownloaderAsync(pkgIdentity, cache, logger, cancelToken);
							await downloader.CopyNupkgFileToAsync(destFile, cancelToken);
						}

						return new PackageArchiveReader(File.OpenRead(destFile));
					}
					catch (Exception ex)
					{
						Util.Exception(ex);
					}
				}

				return null;
			}

			bool foundAll = false;

			if (await byIdRes.DoesPackageExistAsync(packageId, matchingExplicitVersion, cache, logger, cancelToken))
			{
				foundAll = true;

				var destFile = Path.Combine(directory, $"{packageId}.{matchingExplicitVersion}.nupkg");

				AnsiConsole.Markup($"  -> {packageId} {matchingExplicitVersion} ... ");

				var packageReader = await download(destFile, new PackageIdentity(packageId, matchingExplicitVersion));

				if (packageReader == null)
				{
					AnsiConsole.MarkupLine($"{Icon.Error}");
					return false;
				}

				AnsiConsole.MarkupLine($"{Icon.Success}");

				if (includeDependencies)
				{
					var dependencyGroups = new List<PackageDependencyGroup>();

					dependencyGroups.AddRange(await packageReader.GetPackageDependenciesAsync(cancelToken));
					packageReader.Dispose();

					foreach (var depGrp in dependencyGroups)
					{
						foreach (var depPkg in depGrp.Packages)
						{
							var version = depPkg.VersionRange.MinVersion;
							if (!await DownloadPackage(directory, nugetSource, cache, logger, byIdRes, depPkg.Id, version, cancelToken, includeDependencies))
								foundAll = false;
						}
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

		public async Task ParseManifestPackages(string directory, List<Manifest.DotNetWorkload> workloads, CancellationToken cancelToken)
		{
			foreach (var workload in workloads)
			{
				AnsiConsole.MarkupLine($"Reading Workload Manifest: {workload.PackageId} {workload.Version} ...");

				var manifestDirName = Regex.Replace(
						workload.PackageId,
						@"\.Manifest-\d+\.\d+\.\d+$",
						string.Empty,
						RegexOptions.Singleline | RegexOptions.IgnoreCase)
							?.ToLowerInvariant();

				var manifestDir = Path.Combine(ManifestPath, manifestDirName);

				Directory.CreateDirectory(manifestDir);

				var nupkgFile = Path.Combine(directory, $"{workload.PackageId}.{workload.Version}.nupkg");

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
			}
		}

		public IEnumerable<string> GetManifestDirectories()
			=> manifestDirs.Select(m => m.dir);

		public IEnumerable<(string manifestId, string informationalPath, Func<Stream> openManifestStream, Func<Stream> openLocalizationStream)> GetManifests()
					=> manifestDirs.Select(m => (m.id, m.dir, new Func<Stream>(() => File.OpenRead(m.file)), new Func<Stream>(() => null)));

		public string GetSdkFeatureBand()
			=> $"{SdkVersion.Major}.{SdkVersion.Minor}.{SdkVersion.Patch}";
    }
}
