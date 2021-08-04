using Microsoft.DotNet.MSBuildSdkResolver;
using Microsoft.NET.Sdk.WorkloadManifestReader;
using NuGet.Common;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Packaging.Signing;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using Polly;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Newtonsoft.Json.Linq;
using DotNetCheck.Models;

namespace DotNetCheck.DotNet
{
	public class DotNetWorkloadManagerLegacy

	{
		public DotNetWorkloadManagerLegacy(string sdkRoot, string sdkVersion, params string[] nugetPackageSources)
		{
			SdkRoot = sdkRoot;
			SdkVersion = sdkVersion;
			NuGetPackageSources = nugetPackageSources;

			CleanEmptyWorkloadDirectories(sdkRoot, sdkVersion);

			DotNetCliWorkingDir = Path.Combine(Path.GetTempPath(), "maui-check-net-working-dir");
			Directory.CreateDirectory(DotNetCliWorkingDir);

			var globalJson = new DotNetGlobalJson();
			globalJson.Sdk.Version = sdkVersion;
			globalJson.Sdk.RollForward = "disable";
			globalJson.Sdk.AllowPrerelease = true;
			File.WriteAllText(Path.Combine(DotNetCliWorkingDir, "global.json"), globalJson.ToJson());
		}

		public readonly string SdkRoot;
		public readonly string SdkVersion;

		public readonly string[] NuGetPackageSources;

		readonly string DotNetCliWorkingDir;

		void CleanEmptyWorkloadDirectories(string sdkRoot, string sdkVersion)
		{
			if (NuGetVersion.TryParse(sdkVersion, out var v))
			{
				var sdkBand = $"{v.Major}.{v.Minor}.{v.Patch}";

				var manifestsDir = Path.Combine(sdkRoot, "sdk-manifests", sdkBand);

				if (Directory.Exists(manifestsDir))
				{
					foreach (var dir in Directory.GetDirectories(manifestsDir))
					{
						var manifestFile = Path.Combine(dir, "WorkloadManifest.json");

						if (!File.Exists(manifestFile))
						{
							try { Util.Delete(dir, false); }
							catch { }
						}
					}
				}
			}
		}

		async Task DeleteExistingWorkloads(string sdkRoot, string sdkVersion, string workloadIdentifier)
		{
			// Run dotnet workload uninstall first on the workload id
			try
			{
				Util.Log($"Running workload uninstall for {workloadIdentifier}");

				var dotnetExe = Path.Combine(sdkRoot, DotNetSdk.DotNetExeName);

				var args = new[] { "workload", "uninstall", workloadIdentifier };

				await Util.WrapShellCommandWithSudo(dotnetExe, DotNetCliWorkingDir, true, args.ToArray());
			}
			catch (Exception ex)
			{
				Util.Exception(ex);
			}

			if (NuGetVersion.TryParse(sdkVersion, out var v))
			{
				var sdkBand = $"{v.Major}.{v.Minor}.{v.Patch}";

				// Try and clean up the metadata dir too
				var metadataMarkerFile = Path.Combine(sdkRoot, "metadata", "workloads", sdkBand, "InstalledWorkloads", workloadIdentifier);

				if (File.Exists(metadataMarkerFile))
				{
					try { File.Delete(metadataMarkerFile); }
					catch { }
				}

				var manifestsDir = Path.Combine(sdkRoot, "sdk-manifests", sdkBand);

				if (Directory.Exists(manifestsDir))
				{
					foreach (var dir in Directory.GetDirectories(manifestsDir))
					{
						var delete = false;
						var manifestFile = Path.Combine(dir, "WorkloadManifest.json");

						if (File.Exists(manifestFile))
						{
							var json = JObject.Parse(File.ReadAllText(manifestFile));
							var workloadsJson = json["workloads"];

							foreach (var wj in workloadsJson.Children())
							{
								var wid = (wj as JProperty)?.Name;

								if (wid == workloadIdentifier)
								{
									delete = true;
									break;
								}
							}
		
							if (delete)
							{
								Util.Log($"Existing workload with id: {workloadIdentifier} found, deleting...");

								try { Util.Delete(dir, false); }
								catch { }
							}
						}
					}
				}
			}
		}

		public IEnumerable<(string id, string version)> GetInstalledWorkloads()
		{
			var manifestProvider = new SdkDirectoryWorkloadManifestProvider(SdkRoot, SdkVersion);

			foreach (var manifestInfo in manifestProvider.GetManifests())
			{
				using (var manifestStream = manifestInfo.openManifestStream())
				{
					var m = WorkloadManifestReader.ReadWorkloadManifest(manifestInfo.manifestId, manifestStream);

					// Each workload manifest can have one or more workloads defined
					foreach (var wl in m.Workloads)
						yield return (wl.Key.ToString(), m.Version);
				}
			}
		}

		public IEnumerable<WorkloadResolver.PackInfo> GetPacksInWorkload(string workloadId)
		{
			var workloadResolver = WorkloadResolver.Create(new SdkDirectoryWorkloadManifestProvider(SdkRoot, SdkVersion), SdkRoot, SdkVersion);

			var wid = new Microsoft.NET.Sdk.WorkloadManifestReader.WorkloadId(workloadId);
			var packs = workloadResolver.GetPacksInWorkload(wid);
			foreach (var p in packs)
			{
				var packInfo = workloadResolver.TryGetPackInfo(p);
				if (packInfo != null)
					yield return packInfo;
			}
		}

		public async Task CliInstall(IEnumerable<string> workloadIds)
		{
			// dotnet workload install id --skip-manifest-update --add-source x
			var dotnetExe = Path.Combine(SdkRoot, DotNetSdk.DotNetExeName);

			// Arg switched to --source in >= preview 7
			var addSourceArg = "--source";
			if (NuGetVersion.Parse(SdkVersion) <= DotNetCheck.Manifest.DotNetSdk.Version6Preview6)
				addSourceArg = "--add-source";

			var args = new List<string>();
			args.Add("workload");
			args.Add("install");
			args.AddRange(workloadIds);
			args.Add("--skip-manifest-update");
			args.AddRange(NuGetPackageSources.Select(ps => $"{addSourceArg} \"{ps}\""));

			var r = await Util.WrapShellCommandWithSudo(dotnetExe, DotNetCliWorkingDir, true, args.ToArray());

			// Throw if this failed with a bad exit code
			if (r.ExitCode != 0)
			{
				throw new Exception("Failed to install workload: " + string.Join(", ", workloadIds));
			}
		}

		public async Task UninstallTemplate(string templatePackId)
		{
			// dotnet new --uninstall <template>
			var dotnetExe = Path.Combine(SdkRoot, DotNetSdk.DotNetExeName);

			var args = new[] { "new", "--uninstall", templatePackId };

			await Util.WrapShellCommandWithSudo(dotnetExe, DotNetCliWorkingDir, false, args.ToArray());
		}

		public async Task<bool> InstallWorkloadManifest(string packageId, string workloadId, NuGetVersion manifestPackageVersion, CancellationToken cancelToken)
		{
			await DeleteExistingWorkloads(SdkRoot, SdkVersion, workloadId);

			var manifestRoot = GetSdkManifestRoot();

			return await AcquireNuGet(packageId, manifestPackageVersion, manifestRoot, false, cancelToken, true);
		}

		string GetSdkManifestRoot()
		{
			int last2DigitsTo0(int versionBuild)
				=> versionBuild / 100 * 100;

			string manifestDirectory;

			if (!Version.TryParse(SdkVersion.Split('-')[0], out var result))
				throw new ArgumentException("Invalid 'SdkVersion' version: " + SdkVersion);

			var sdkVersionBand = $"{result.Major}.{result.Minor}.{last2DigitsTo0(result.Build)}";
			var environmentVariable = Environment.GetEnvironmentVariable("DOTNETSDK_WORKLOAD_MANIFEST_ROOT");
			if (!string.IsNullOrEmpty(environmentVariable))
				manifestDirectory = environmentVariable;
			else
				manifestDirectory = Path.Combine(SdkRoot, "sdk-manifests", sdkVersionBand);

			return manifestDirectory;
		}

		static async Task<bool> DownloadAndExtractNuGet(SourceRepository nugetSource, SourceCacheContext cache, ILogger logger, string packageId, NuGetVersion packageVersion, string destination, bool appendVersionToExtractPath, CancellationToken cancelToken)
		{
			var deleteAfter = false;
			var tmpPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

			try
			{
				Directory.CreateDirectory(tmpPath);
				deleteAfter = true;
			}
			catch
			{
				tmpPath = Path.GetTempPath();
			}

			var packageIdentity = new PackageIdentity(packageId, packageVersion);

			var nugetSettings = NuGet.Configuration.NullSettings.Instance;

			var byIdRes = await nugetSource.GetResourceAsync<FindPackageByIdResource>();

			// Cause a retry if this is null
			if (byIdRes == null)
				throw new InvalidDataException();

			if (await byIdRes.DoesPackageExistAsync(packageId, packageVersion, cache, logger, cancelToken))
			{
				var downloaderResource = await nugetSource.GetResourceAsync<DownloadResource>(cancelToken);

				using var downloader = await byIdRes.GetPackageDownloaderAsync(packageIdentity, cache, logger, cancelToken);

				var downloadContext = new PackageDownloadContext(cache, tmpPath, true);

				// Download the package (might come from the shared package cache).
				var downloadResult = await downloaderResource.GetDownloadResourceResultAsync(
					new PackageIdentity(packageId, packageVersion),
					downloadContext,
					tmpPath,
					logger,
					cancelToken);

				var clientPolicy = ClientPolicyContext.GetClientPolicy(nugetSettings, logger);

				var packagePathResolver = new DotNetSdkPackPackagePathResolver(destination, appendVersionToExtractPath, true);

				var fullDestinationDir = packagePathResolver.GetInstallPath(packageIdentity);

				// Try and delete the destination if it exists first
				try
				{
					if (Directory.Exists(fullDestinationDir))
						Util.Delete(fullDestinationDir, false);
				}
				catch (Exception ex)
				{
					Util.Exception(ex);
				}

				var packageExtractionContext = new PackageExtractionContext(
					PackageSaveMode.Files | PackageSaveMode.Nuspec,
					XmlDocFileSaveMode.Skip,
					clientPolicy,
					logger);

				// Extract the package into the target directory.
				await PackageExtractor.ExtractPackageAsync(
					downloadResult.PackageSource,
					downloadResult.PackageStream,
					packagePathResolver,
					packageExtractionContext,
					cancelToken);

				// Check for data/WorkloadManifest.json and data/WorkloadManifest.targets
				var dataDir = Path.Combine(fullDestinationDir, "data");

				if (Directory.Exists(dataDir))
				{
					try { Util.DirectoryCopy(dataDir, fullDestinationDir, true); }
					catch (Exception ex) { Util.Exception(ex); }
				}

				try { Directory.Delete(dataDir, true); }
				catch (Exception ex) { Util.Exception(ex); }

				// Try deleting files that need not be kept after extracting
				try
				{
					var files = Directory.GetFiles(fullDestinationDir);

					foreach (var f in files)
					{
						var fileName = Path.GetFileName(f);
						if (fileName.Equals("WorkloadManifest.json")
							|| fileName.Equals("WorkloadManifest.targets"))
							continue;

						try { Util.Delete(f, true); }
						catch { }
					}
				}
				catch { }

				try
				{
					if (deleteAfter)
						Directory.Delete(tmpPath, true);
				}
				catch (Exception ex) { Util.Exception(ex); }

				return true;
			}

			return false;
		}

		class DotNetSdkPackPackagePathResolver : PackagePathResolver
		{
			public DotNetSdkPackPackagePathResolver(string rootDirectory, bool appendVersionToPath, bool isWorkload)
				: base(rootDirectory, false)
			{
				AppendVersionToPath = appendVersionToPath;
				IsWorkload = isWorkload;
			}

			public readonly bool AppendVersionToPath;
			public readonly bool IsWorkload;

			public override string GetPackageDirectoryName(PackageIdentity packageIdentity)
				=> GetPathBase(packageIdentity).ToString();

			public override string GetPackageFileName(PackageIdentity packageIdentity)
				=> GetPathBase(packageIdentity) + PackagingCoreConstants.NupkgExtension;

			string GetPathBase(PackageIdentity packageIdentity)
			{
				if (AppendVersionToPath)
					return Path.Combine(packageIdentity.Id, packageIdentity.Version.ToString());

				// Workloads have a package id that ends with .Manifest-x.y.z but we don't actually
				// want that part in the path we install the workload to
				// Let's replace
				if (IsWorkload)
				{
					return Regex.Replace(
						packageIdentity.Id,
						@"\.Manifest-\d+\.\d+\.\d+$",
						string.Empty,
						RegexOptions.Singleline | RegexOptions.IgnoreCase)
							?.ToLowerInvariant();
				}

				return packageIdentity.Id;
			}
		}

		async Task<bool> DownloadNuGet(SourceRepository nugetSource, SourceCacheContext cache, ILogger logger, string packageId, NuGetVersion packageVersion, string destination, CancellationToken cancelToken)
		{
			var byIdRes = await nugetSource.GetResourceAsync<FindPackageByIdResource>();

			// Cause a retry if this is null
			if (byIdRes == null)
				throw new InvalidDataException();

			if (await byIdRes.DoesPackageExistAsync(packageId, packageVersion, cache, logger, cancelToken))
			{
				using var downloader = await byIdRes.GetPackageDownloaderAsync(new PackageIdentity(packageId, packageVersion), cache, logger, cancelToken);

				if (downloader == null)
					throw new InvalidDataException();

				// If the file exists in the destination, delete it first
				try
				{
					if (File.Exists(destination))
						Util.Delete(destination, true);
				}
				catch (Exception ex)
				{
					Util.Exception(ex);
				}

				await downloader.CopyNupkgFileToAsync(destination, cancelToken);

				return true;
			}

			return false;
		}

		async Task<bool> AcquireNuGet(string packageId, NuGetVersion packageVersion, string destination, bool appendVersionToExtractPath, CancellationToken cancelToken, bool extract)
		{
			var nugetCache = NullSourceCacheContext.Instance;
			var nugetLogger = NullLogger.Instance;

			var ok = false;

			foreach (var pkgSrc in NuGetPackageSources)
			{
				var nugetSource = Repository.Factory.GetCoreV3(pkgSrc);

				try
				{
					var result = await Policy
						.Handle<ObjectDisposedException>()
						.Or<OperationCanceledException>()
						.Or<IOException>()
						.Or<InvalidDataException>()
						.RetryAsync(3)
						.ExecuteAsync(async () =>
						{
							if (extract)
							{
								return await Util.WrapCopyWithShellSudo(destination, false, d =>
									DownloadAndExtractNuGet(
										nugetSource,
										nugetCache,
										nugetLogger,
										packageId,
										packageVersion,
										d,
										appendVersionToExtractPath,
										cancelToken));
							}
							else
							{
								return await Util.WrapCopyWithShellSudo(destination, true, d =>
									DownloadNuGet(
										nugetSource,
										nugetCache,
										nugetLogger,
										packageId,
										packageVersion,
										d,
										cancelToken));
							}
						});

					// We just need one success from any of the package sources
					if (result)
					{
						ok = true;

						// If we installed from this package source, no need to try any others
						break;
					}
				}
				catch (Exception ex)
				{
					Util.Exception(ex);
					throw ex;
				}
			}

			return ok;
		}
	}
}
