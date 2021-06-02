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

namespace DotNetCheck.DotNet
{
	public class DotNetWorkloadManager
	{
		public DotNetWorkloadManager(string sdkRoot, string sdkVersion, params string[] nugetPackageSources)
		{
			SdkRoot = sdkRoot;
			SdkVersion = sdkVersion;
			NuGetPackageSources = nugetPackageSources;

			CleanEmptyWorkloadDirectories(sdkRoot, sdkVersion);

			manifestProvider = new SdkDirectoryWorkloadManifestProvider(SdkRoot, SdkVersion);
			workloadResolver = WorkloadResolver.Create(manifestProvider, SdkRoot, SdkVersion);
		}

		public readonly string SdkRoot;
		public readonly string SdkVersion;

		readonly SdkDirectoryWorkloadManifestProvider manifestProvider;
		readonly WorkloadResolver workloadResolver;

		public readonly string[] NuGetPackageSources;


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

		public IEnumerable<(string packageId, NuGetVersion packageVersion)> GetInstalledWorkloadNuGetPackages()
		{
			foreach (var dir in manifestProvider.GetManifestDirectories())
			{
				if (Directory.Exists(dir))
				{
					var nuspec = Directory.EnumerateFiles(dir, "*.nuspec", SearchOption.TopDirectoryOnly)?.FirstOrDefault();

					if (nuspec != null)
					{
						var xml = new XmlDocument();
						xml.Load(nuspec);

						var nsUri = xml.DocumentElement.NamespaceURI;
						var nsManager = new XmlNamespaceManager(new NameTable());
						nsManager.AddNamespace("nuspec", nsUri);

						var idNode = xml.SelectSingleNode("/nuspec:package/nuspec:metadata/nuspec:id", nsManager);
						var versionNode = xml.SelectSingleNode("/nuspec:package/nuspec:metadata/nuspec:version", nsManager);

						if (idNode != null && versionNode != null)
						{
							var id = idNode.InnerText;
							var ver = versionNode.InnerText;

							if (!string.IsNullOrEmpty(id) && NuGetVersion.TryParse(ver, out var v))
								yield return (id, v);
						}
					}
				}
			}
		}

		//public IEnumerable<(string id, Int64 version)> GetInstalledWorkloads()
		//{
		//	var workloadManifestReaderType = typeof(WorkloadResolver).Assembly.GetType("Microsoft.NET.Sdk.WorkloadManifestReader.WorkloadManifestReader");

		//	var themethod = workloadManifestReaderType.GetMethod("ReadWorkloadManifest", BindingFlags.Public | BindingFlags.Static, Type.DefaultBinder, new Type[] { typeof(Stream) }, null);

		//	var readWorkloadManifestMethods = workloadManifestReaderType.GetMethods(BindingFlags.Static | BindingFlags.Public)

		//		.FirstOrDefault(m => m.Name == "ReadWorkloadManifest" && (m.GetParameters()?.FirstOrDefault()?.Equals(typeof(Stream)) ?? false) && (m.GetParameters()?.Length ?? 0) == 1);

		//	var list = new List<object>(); // WorkloadManifest
		//	foreach (Stream manifest in manifestProvider.GetManifests())
		//	{
		//		using (manifest)
		//		{
		//			var workloadManifest = themethod.Invoke(null, new object[] { manifest });

		//			var workloadsDict = workloadManifest.GetType().GetProperty("Workloads").GetValue(workloadManifest);

		//			var workloadVersion = (Int64)workloadManifest.GetType().GetProperty("Version").GetValue(workloadManifest);


		//			var workloadsDictKeys = workloadsDict.GetType().GetProperty("Keys").GetValue(workloadsDict) as System.Collections.ICollection;

		//			foreach (var key in workloadsDictKeys)
		//			{
		//				var workloadId = key.ToString();

		//				yield return (workloadId, workloadVersion);
		//			}
		//		}
		//	}
		//}

		public IEnumerable<WorkloadResolver.PackInfo> GetPacksInWorkload(string workloadId)
		{
			foreach (var packId in workloadResolver.GetPacksInWorkload(workloadId))
			{
				var packInfo = workloadResolver.TryGetPackInfo(packId);
				if (packInfo != null)
					yield return packInfo;
			}
		}

		public ISet<WorkloadResolver.WorkloadInfo> GetWorkloadSuggestions(params string[] missingPackIds)
			=> workloadResolver.GetWorkloadSuggestionForMissingPacks(missingPackIds);

		public IEnumerable<WorkloadResolver.PackInfo> GetAllInstalledWorkloadPacks()
			=> workloadResolver.GetInstalledWorkloadPacksOfKind(WorkloadPackKind.Framework)
				.Concat(workloadResolver.GetInstalledWorkloadPacksOfKind(WorkloadPackKind.Library))
				.Concat(workloadResolver.GetInstalledWorkloadPacksOfKind(WorkloadPackKind.Sdk))
				.Concat(workloadResolver.GetInstalledWorkloadPacksOfKind(WorkloadPackKind.Template))
				.Concat(workloadResolver.GetInstalledWorkloadPacksOfKind(WorkloadPackKind.Tool));

		public IEnumerable<WorkloadResolver.PackInfo> GetInstalledWorkloadPacks(WorkloadPackKind kind)
			=> workloadResolver.GetInstalledWorkloadPacksOfKind(kind);

		public Task<bool> InstallWorkloadManifest(string packageId, NuGetVersion manifestPackageVersion, CancellationToken cancelToken)
		{
			var manifestRoot = GetSdkManifestRoot();

			return AcquireNuGet(packageId, manifestPackageVersion, manifestRoot, false, cancelToken, true, isWorkload: true);
		}

		public bool PackExistsOnDisk(string packId, string packVersion)
		{
			var packFolder = Path.Combine(SdkRoot, "packs", packId, packVersion);

			try
			{
				if (Directory.Exists(packFolder)
					&& (Directory.EnumerateFiles(packFolder, $"{packId}*.nuspec", SearchOption.AllDirectories).Any()))
					return true;
			}
			catch (Exception ex)
			{
				Util.Exception(ex);
			}

			return false;
		}

		public bool TemplateExistsOnDisk(string packId, string packVersion, string packKind, string templateShortName = null)
		{
			var sdkTemplatePacksFolder = Path.Combine(SdkRoot, "template-packs");

			Util.Log($"Looking for template pack on disk: {sdkTemplatePacksFolder}");

			try
			{
				if (Directory.Exists(sdkTemplatePacksFolder)
					&& (Directory.EnumerateFiles(sdkTemplatePacksFolder, $"{packId}.{packVersion}*.nupkg", SearchOption.AllDirectories).Any()
					|| Directory.EnumerateFiles(sdkTemplatePacksFolder, $"{packId}.{packVersion}*.nupkg".ToLowerInvariant(), SearchOption.AllDirectories).Any()))
				{
					Util.Log($"Found pack on disk: {sdkTemplatePacksFolder}");
					return true;
				}
			}
			catch (Exception ex)
			{
				Util.Exception(ex);
			}

			try
			{
				var templateEngineSdksDir = Path.Combine(
					Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
					".templateengine",
					"dotnetcli");

				if (Directory.Exists(templateEngineSdksDir))
				{
					var templateEngineSdkVersionDirs = Directory.EnumerateDirectories(templateEngineSdksDir, "v*", SearchOption.TopDirectoryOnly);

					var versionDirs = new List<string>();

					foreach (var sdkVerDir in templateEngineSdkVersionDirs)
					{
						var strVer = new DirectoryInfo(sdkVerDir).Name.Substring(1);
						if (NuGetVersion.TryParse(strVer, out var v))
							versionDirs.Add(strVer);
					}

					var newestVersion = versionDirs.OrderByDescending(v => v).FirstOrDefault();

					var userTemplateEngineDir = Path.Combine(
						templateEngineSdksDir,
						$"v{newestVersion}",
						"packages");

					Util.Log($"Newest dotnet sdk version to look for templates in: {userTemplateEngineDir}");

					if (Directory.Exists(userTemplateEngineDir)
						&& (Directory.EnumerateFiles(userTemplateEngineDir, $"{packId}.{packVersion}*.nupkg", SearchOption.AllDirectories).Any()
						|| Directory.EnumerateFiles(userTemplateEngineDir, $"{packId}.{packVersion}*.nupkg".ToLowerInvariant(), SearchOption.AllDirectories).Any()))
					{
						Util.Log($"Found pack on disk: {userTemplateEngineDir}");
						return true;
					}


					var templateEnginePkgsDir = Path.Combine(
						Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
						".templateengine",
						"packages");

					if (Directory.Exists(templateEnginePkgsDir)
						&& (Directory.EnumerateFiles(templateEnginePkgsDir, $"{packId}.{packVersion}*.nupkg", SearchOption.AllDirectories).Any()
						|| Directory.EnumerateFiles(templateEnginePkgsDir, $"{packId}.{packVersion}*.nupkg".ToLowerInvariant(), SearchOption.AllDirectories).Any()))
					{
						Util.Log($"Found pack on disk: {templateEnginePkgsDir}");
						return true;
					}
				}

			}
			catch (Exception ex)
			{
				Util.Exception(ex);
			}

			return false;
		}

		public async Task<bool> InstallWorkloadPack(string sdkRoot, Manifest.DotNetSdkPack sdkPack, CancellationToken cancelToken)
		{
			WorkloadResolver.PackInfo packInfo;

			if (sdkPack.SkipManifestCheck && NuGetVersion.TryParse(sdkPack.Version, out var packVersion))
			{
				var kind = sdkPack?.PackKind?.ToLowerInvariant() switch
				{
					"sdk" => WorkloadPackKind.Sdk,
					"framework" => WorkloadPackKind.Framework,
					"library" => WorkloadPackKind.Library,
					"template" => WorkloadPackKind.Template,
					"tool" => WorkloadPackKind.Tool,
					_ => WorkloadPackKind.Sdk
				};

				var path = kind == WorkloadPackKind.Template ?
					Path.Combine(Path.GetTempPath(), $"{sdkPack.Id}.{sdkPack.Version}.nupkg")
					: Path.Combine(sdkRoot, "sdk", $"{sdkPack.Id}", sdkPack.Version);

				packInfo = new WorkloadResolver.PackInfo(sdkPack.Id, sdkPack.Version, kind, path);
			}
			else
			{
				packInfo = workloadResolver.TryGetPackInfo(sdkPack.Id);
			}

			if (packInfo != null && NuGetVersion.TryParse(packInfo.Version, out var version))
			{
				if (packInfo.Kind == WorkloadPackKind.Template)
				{
					var r = await AcquireNuGet(packInfo.Id, version, packInfo.Path, false, cancelToken, false, false);

					// Short circuit the installation into the template-packs dir since this one might not
					// be a part of any workload manifest, so we need to install with dotnet new -i
					if (sdkPack.SkipManifestCheck)
					{
						var dotnetExe = Path.Combine(sdkRoot, DotNetSdk.DotNetExeName);

						var p = new ShellProcessRunner(new ShellProcessRunnerOptions(dotnetExe, $"new -i \"{packInfo.Path}\""));
						var pr = p.WaitForExit();

						if (pr.GetOutput().Contains("permission denied", StringComparison.InvariantCultureIgnoreCase))
							throw new ApplicationException($"Your templates cache folder has some invalid permissions.  Please try to delete the folder and try again: "
								+ Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".templateengine", "dotnetcli"));

						return pr?.ExitCode == 0;
					}

					return r;
				}

				var actualPackId = GetAliasToPackId(packInfo);

				if (!string.IsNullOrEmpty(actualPackId))
				{
					var packsRoot = Path.Combine(SdkRoot, "packs");

					return await AcquireNuGet(actualPackId, version, packsRoot, true, cancelToken, true, false);
				}
			}

			return false;
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

		string GetAliasToPackId(WorkloadResolver.PackInfo packInfo)
			=> GetAliasToPackId(packInfo.Path, packInfo.Id, packInfo.Version);

		string GetAliasToPackId(string packPath, string packId, string packVersion)
		{
			if (NuGetVersion.TryParse(packVersion, out var nugetVersion))
			{
				if (Uri.TryCreate($"file://{packPath}", UriKind.Absolute, out var pathUri))
				{
					var pathSegments = pathUri.Segments.Select(s => s.Trim('/'));

					// Check if the segment is equal to, or starts with the manifest package id
					// since the id we have might be one with an alias-to and we want the alias id instead, to restore that 
					var aliasOrId = pathSegments.FirstOrDefault(p => p.StartsWith(packId, StringComparison.OrdinalIgnoreCase));

					if (!string.IsNullOrEmpty(aliasOrId))
					{
						if (aliasOrId.EndsWith(".nupkg", StringComparison.OrdinalIgnoreCase))
							aliasOrId = aliasOrId.Substring(0, aliasOrId.Length - 6);
						
						return aliasOrId;
					}
				}
			}

			if (packId.EndsWith(".nupkg", StringComparison.OrdinalIgnoreCase))
				packId = packId.Substring(0, packId.Length - 6);

			return packId;
		}

		static async Task<bool> DownloadAndExtractNuGet(SourceRepository nugetSource, SourceCacheContext cache, ILogger logger, string packageId, NuGetVersion packageVersion, string destination, bool appendVersionToExtractPath, CancellationToken cancelToken, bool isWorkload)
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

				var packagePathResolver = new DotNetSdkPackPackagePathResolver(destination, appendVersionToExtractPath, isWorkload);

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
				if (isWorkload)
				{
					var dataDir = Path.Combine(fullDestinationDir, "data");

					if (Directory.Exists(dataDir))
					{
						try { Util.DirectoryCopy(dataDir, fullDestinationDir, true); }
						catch (Exception ex) { Util.Exception(ex); }
					}

					try { Directory.Delete(dataDir, true); }
					catch (Exception ex) { Util.Exception(ex); }
				}

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
					return Regex.Replace(packageIdentity.Id, @"\.Manifest-\d+\.\d+\.\d+$", string.Empty, RegexOptions.Singleline);
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

		async Task<bool> AcquireNuGet(string packageId, NuGetVersion packageVersion, string destination, bool appendVersionToExtractPath, CancellationToken cancelToken, bool extract, bool isWorkload)
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
										cancelToken,
										isWorkload));
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
