using DotNetCheck.DotNet;
using DotNetCheck.Models;
using DotNetCheck.Solutions;
using NuGet.Versioning;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNetCheck.Checkups
{
	public class DotNetWorkloadDuplicatesCheckup : Checkup
	{
		public DotNetWorkloadDuplicatesCheckup() : base()
		{
		}

		public override string Id => "dotnet-workload-dedup";

		public override string Title => ".NET SDK - Workload Deduplication";

		public override IEnumerable<CheckupDependency> DeclareDependencies(IEnumerable<string> checkupIds)
			=> new[] {
				new CheckupDependency("dotnet")
			};

		public override Task<DiagnosticResult> Examine(SharedState history)
		{
			var dotnet = new DotNetSdk(history);
			var sdkRoot = dotnet.DotNetSdkLocation.FullName;

			if (history.TryGetEnvironmentVariable("DOTNET_SDK_VERSION", out var sdkVersion))
			{

				// HACK  / WORKAROUND
				// Workload manifest specs changed between p4/p5 and with that came manifest folder name changes
				// The problem is we can be left with duplicate workload manifests but in different
				// folder names and the resolver can't handle that.
				// This will identify a block list of old workload folders and delete them if the dotnet version
				// is -preview.5 or newer of the sdk and a different block list if -preview.4 or older
				if (NuGetVersion.TryParse(sdkVersion, out var parsedSdkVersion))
				{
					DeleteBlockedWorkloads(
						sdkRoot,
						$"{parsedSdkVersion.Major}.{parsedSdkVersion.Minor}.{parsedSdkVersion.Patch}",
						parsedSdkVersion >= new NuGetVersion("6.0.100-preview.5")
						? new[] {
							"Microsoft.NET.Workload.Android",
							"Microsoft.NET.Workload.BlazorWebAssembly",
							"Microsoft.NET.Workload.iOS",
							"Microsoft.NET.Workload.MacCatalyst",
							"Microsoft.NET.Workload.macOS",
							"Microsoft.NET.Workload.tvOS" }
						: new[] {
							"Microsoft.NET.Sdk.Android",
							"Microsoft.NET.Sdk.iOS",
							"Microsoft.NET.Sdk.MacCatalyst",
							"Microsoft.NET.Sdk.macOS",
							"Microsoft.NET.Sdk.tvOS",
							"Microsoft.NET.Workload.Mono.ToolChain" });
				}
			}

			return Task.FromResult(DiagnosticResult.Ok(this));
		}

		void DeleteBlockedWorkloads(string sdkRoot, string sdkVersionBand, string[] blockedWorkloadFolderNames)
		{
			foreach (var b in blockedWorkloadFolderNames)
			{
				var manifestFolderPath = Path.Combine(sdkRoot, "sdk-manifests", sdkVersionBand, b);

				if (Directory.Exists(manifestFolderPath))
				{
					Util.Log($"Deleting old workload manifest: {b}");

					try { Util.Delete(manifestFolderPath, false); }
					catch (Exception ex)
					{
						Util.Exception(ex);
					}
				}
			}
		}
	}
}
