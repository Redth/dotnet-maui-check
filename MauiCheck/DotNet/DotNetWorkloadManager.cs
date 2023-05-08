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
using System.Net.Http;
using System.Text.Json.Nodes;

namespace DotNetCheck.DotNet
{
	public class DotNetWorkloadManager
	{
		public DotNetWorkloadManager(string sdkRoot, string sdkVersion, params string[] nugetPackageSources)
		{
			SdkRoot = sdkRoot;
			SdkVersion = sdkVersion;
			NuGetPackageSources = nugetPackageSources;

			DotNetCliWorkingDir = Path.Combine(Path.GetTempPath(), "maui-check-" + Guid.NewGuid().ToString("N").Substring(0, 8));
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

		public async Task Repair()
		{
			await CliRepair();
		}

		public async Task Install(string rollbackJson, string[] workloadIds)
		{
			await CliInstallWithRollback(rollbackJson, workloadIds);
		}

		public Dictionary<string, NuGetVersion> GetInstalledWorkloadManifestIdsAndVersions()
		{
			var items = new Dictionary<string, NuGetVersion>();

			var manifestProvider = new SdkDirectoryWorkloadManifestProvider(SdkRoot, SdkVersion, null);

			foreach (var manifestInfo in manifestProvider.GetManifests())
			{
				using (var manifestStream = manifestInfo.OpenManifestStream())
				{
					var m = WorkloadManifestReader.ReadWorkloadManifest(manifestInfo.ManifestId, manifestStream, manifestInfo.ManifestPath);

					if (NuGetVersion.TryParse(m.Version, out var v))
						items[manifestInfo.ManifestId] = v;
				}
			}

			return items;
		}

		public Dictionary<string, (NuGetVersion Version, NuGetVersion SdkBand)> ParseRollback(string rollbackJson)
		{
			var results = new Dictionary<string, (NuGetVersion Version, NuGetVersion SdkBand)>();
			var j = JObject.Parse(rollbackJson);

			foreach (var p in j.Properties())
			{
				if (string.IsNullOrEmpty(p.Name))
					continue;

				var version = p.Value?.Value<string>();

				if (string.IsNullOrEmpty(version))
					continue;

				var versionParts = version.Split('/', 2, StringSplitOptions.RemoveEmptyEntries);
				if (versionParts is null || versionParts.Length != 2)
					continue;

				if (!NuGetVersion.TryParse(versionParts[0], out var v) || !NuGetVersion.TryParse(versionParts[1], out var sdkBand))
					continue;

				results.Add(p.Name, (v, sdkBand));
			}

			return results;
		}

		public IEnumerable<(string id, string version)> GetInstalledWorkloads()
		{
			var manifestProvider = new SdkDirectoryWorkloadManifestProvider(SdkRoot, SdkVersion, null);

			foreach (var manifestInfo in manifestProvider.GetManifests())
			{
				using (var manifestStream = manifestInfo.OpenManifestStream())
				{
					var m = WorkloadManifestReader.ReadWorkloadManifest(manifestInfo.ManifestId, manifestStream, manifestInfo.ManifestPath);

					// Each workload manifest can have one or more workloads defined
					foreach (var wl in m.Workloads)
						yield return (wl.Key.ToString(), m.Version);
				}
			}
		}

		async Task CliInstallWithRollback(string rollbackJson, IEnumerable<string> workloadIds)
		{
			var rollbackFile = Path.Combine(DotNetCliWorkingDir, "rollback.json");
			File.WriteAllText(rollbackFile, rollbackJson);

			// dotnet workload install id --from-rollback-file --source x
			var dotnetExe = Path.Combine(SdkRoot, DotNetSdk.DotNetExeName);

			var args = new List<string>
			{
				"workload",
				"install",
				// "--no-cache",
				// "--disable-parallel"
				"--from-rollback-file",
				$"\"{rollbackFile}\""
				
			};
			args.AddRange(workloadIds);
			args.AddRange(NuGetPackageSources.Select(ps => $"--source \"{ps}\""));

			var r = await Util.WrapShellCommandWithSudo(dotnetExe, DotNetCliWorkingDir, true, args.ToArray());

			// Throw if this failed with a bad exit code
			if (r.ExitCode != 0)
				throw new Exception("Workload Install failed: `dotnet " + string.Join(' ', args) + "`");
		}


		async Task CliRepair()
		{
			// dotnet workload repair --source x
			var dotnetExe = Path.Combine(SdkRoot, DotNetSdk.DotNetExeName);

			var args = new List<string>
			{
				"workload",
				"repair"
			};
			args.AddRange(NuGetPackageSources.Select(ps => $"--source \"{ps}\""));

			var r = await Util.WrapShellCommandWithSudo(dotnetExe, DotNetCliWorkingDir, true, args.ToArray());

			// Throw if this failed with a bad exit code
			if (r.ExitCode != 0)
				throw new Exception("Workload Repair failed: `dotnet " + string.Join(' ', args) + "`");
		}
	}
}
