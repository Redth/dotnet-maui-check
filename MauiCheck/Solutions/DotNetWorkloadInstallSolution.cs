using DotNetCheck.DotNet;
using DotNetCheck.Manifest;
using DotNetCheck.Models;
using NuGet.Versioning;
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

			if (NuGetVersion.TryParse(Workload.Version, out var version)
				&& await workloadManager.InstallWorkloadManifest(Workload.PackageId, Workload.Id, version, cancellationToken))
			{
				// In 6.0-preview.6 or newer, we should use the dotnet cli to acquire the packs for the workload we are installing
				if (NuGetVersion.TryParse(SdkVersion, out var nugetSdkVersion) && nugetSdkVersion >= DotNetCheck.Manifest.DotNetSdk.Version6Preview6)
					await workloadManager.CliInstall(Workload.Id);

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
