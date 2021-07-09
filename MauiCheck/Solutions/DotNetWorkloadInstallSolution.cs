using DotNetCheck.DotNet;
using DotNetCheck.Manifest;
using DotNetCheck.Models;
using NuGet.Versioning;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DotNetCheck.Solutions
{
	public class DotNetWorkloadInstallSolution : Solution
	{
		public DotNetWorkloadInstallSolution(string sdkRoot, string sdkVersion, DotNetWorkload workload, params string[] nugetPackageSources)
		{
			SdkRoot = sdkRoot;
			SdkVersion = sdkVersion;
			NuGetPackageSources = nugetPackageSources;
			Workload = workload;
		}

		public readonly string SdkRoot;
		public readonly string SdkVersion;
		public readonly string[] NuGetPackageSources;

		public readonly DotNetWorkload Workload;

		public override async Task Implement(SharedState sharedState, CancellationToken cancellationToken)
		{
			await base.Implement(sharedState, cancellationToken);

			ReportStatus($"Installing Workload: {Workload.Id}...");

			var workloadManager = new DotNetWorkloadManager(SdkRoot, SdkVersion, NuGetPackageSources);

			if (NuGetVersion.TryParse(Workload.Version, out var version))
			{
				// Manually download and install the manifest to get an explicit version of it to install
				// we have to run `dotnet workload install <id> --skip-manifest-update` to make this happen
				if (await workloadManager.InstallWorkloadManifest(Workload.PackageId, Workload.Id, version, cancellationToken))
				{
					// This runs the `dotnet workload install <id> --skip-manifest-update`
					await workloadManager.CliInstall(Workload.Id);

					// Find any template packs that the workload comes with
					// we want to try and `dotnet new --uninstall` them
					// Since if one was previously installed with `dotnet new -i` it will be chosen over the optional workload
					// version and the user could get a message about a newer template being available to install
					var templatePacks = workloadManager.GetPacksInWorkload(Workload.Id)?.Where(p => p.Kind == Microsoft.NET.Sdk.WorkloadManifestReader.WorkloadPackKind.Template);

					if (templatePacks?.Any() ?? false)
					{
						ReportStatus("Uninstalling previous template versions...");

						foreach (var tp in templatePacks)
						{
							try { await workloadManager.UninstallTemplate(tp.Id); }
							catch { }
						}
					}
				}

				// Install: dotnet workload install id --
				ReportStatus($"Installed Workload: {Workload.Id}.");
			}
			else
			{
				var msg = $"Failed to install workload: {Workload.Id}.";
				ReportStatus(msg);
				throw new System.Exception(msg);
			}
		}
	}
}
