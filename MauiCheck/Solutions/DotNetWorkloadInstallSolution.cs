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
			Workload = workload;
			WorkloadManager = new DotNetWorkloadManager(sdkRoot, sdkVersion, nugetPackageSources);
		}

		public readonly DotNetWorkloadManager WorkloadManager;
		public DotNetWorkload Workload { get; private set; }

		public override bool RequiresAdmin(Platform platform)
		{
			if (platform == Platform.Windows)
				return true;

			return base.RequiresAdmin(platform);
		}

		public override async Task Implement(CancellationToken cancellationToken)
		{
			await base.Implement(cancellationToken);

			ReportStatus($"Installing Workload: {Workload.Id}...");

			if (NuGetVersion.TryParse(Workload.Version, out var version)
				&& await WorkloadManager.InstallWorkloadManifest(Workload.PackageId, version, cancellationToken))
			{
				ReportStatus($"Installed Workload: {Workload.Id}.");
			}
			else
				ReportStatus($"Failed to install workload: {Workload.Id}.");
		}
	}
}
