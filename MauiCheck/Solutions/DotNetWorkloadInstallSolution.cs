using DotNetCheck.DotNet;
using DotNetCheck.Manifest;
using DotNetCheck.Models;
using Microsoft.NET.Sdk.WorkloadManifestReader;
using NuGet.Versioning;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DotNetCheck.Solutions
{
	public class DotNetWorkloadInstallSolution : Solution
	{
		public DotNetWorkloadInstallSolution(string sdkRoot, string sdkVersion, DotNetWorkload[] workloads, params string[] nugetPackageSources)
		{
			SdkRoot = sdkRoot;
			SdkVersion = sdkVersion;
			NuGetPackageSources = nugetPackageSources;
			Workloads = workloads;
		}

		public readonly string SdkRoot;
		public readonly string SdkVersion;
		public readonly string[] NuGetPackageSources;

		public readonly DotNetWorkload[] Workloads;

		public override async Task Implement(SharedState sharedState, CancellationToken cancellationToken)
		{
			await base.Implement(sharedState, cancellationToken);

			var workloadIds = Workloads.Select(w => w.Id);

			ReportStatus($"Installing Workloads: " + string.Join(' ', workloadIds));

			var workloadManager = new DotNetWorkloadManagerLegacy(SdkRoot, SdkVersion, NuGetPackageSources);

			foreach (var workload in Workloads)
			{
				if (NuGetVersion.TryParse(workload.Version, out var version))
				{
					// Manually download and install the manifest to get an explicit version of it to install
					// we have to run `dotnet workload install <id> --skip-manifest-update` to make this happen later
					if (await workloadManager.InstallWorkloadManifest(workload.PackageId, workload.Id, version, cancellationToken))
					{
						// Find any template packs that the workload comes with
						// we want to try and `dotnet new --uninstall` them
						// Since if one was previously installed with `dotnet new -i` it will be chosen over the optional workload
						// version and the user could get a message about a newer template being available to install
						var templatePacks = workloadManager.GetPacksInWorkload(workload.Id)?.Where(p => p.Kind == WorkloadPackKind.Template);

						if (templatePacks?.Any() ?? false)
						{
							ReportStatus($"Uninstalling previous template versions for {workload.Id}...");

							foreach (var tp in templatePacks)
							{
								try { await workloadManager.UninstallTemplate(tp.Id); }
								catch { }
							}
						}
					}
				}
			}

			// This runs the `dotnet workload install <id> <id> ... --skip-manifest-update`
			await workloadManager.CliInstall(workloadIds);
			
			// Install: dotnet workload install id --
			ReportStatus($"Installed Workloads: " + string.Join(' ', workloadIds));
		}
	}
}
