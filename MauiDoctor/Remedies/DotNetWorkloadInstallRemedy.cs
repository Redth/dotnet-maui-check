using MauiDoctor.Manifest;
using NuGet.Versioning;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MauiDoctor.Doctoring
{
	public class DotNetWorkloadInstallRemedy : Remedy
	{
		public DotNetWorkloadInstallRemedy(string sdkRoot, string sdkVersion, DotNetWorkload[] packages, params string[] nugetPackageSources)
		{
			Packages = packages;
			WorkloadManager = new DotNetWorkloadManager(sdkRoot, sdkVersion, nugetPackageSources);
		}

		public readonly DotNetWorkloadManager WorkloadManager;
		public DotNetWorkload[] Packages { get; private set; }

		public override bool RequiresAdmin(Platform platform)
		{
			if (platform == Platform.Windows)
				return true;

			return base.RequiresAdmin(platform);
		}

		public override async Task Cure(CancellationToken cancellationToken)
		{
			await base.Cure(cancellationToken);

			foreach (var pack in Packages)
			{
				ReportStatus($"Installing Workload: {pack.Id}...");

				if (NuGetVersion.TryParse(pack.Version, out var version)
					&& await WorkloadManager.InstallWorkloadManifest(pack.PackageId, version, cancellationToken))
				{
					ReportStatus($"Installed Workload: {pack.Id}.");
				}
				else
					ReportStatus($"Failed to install workload: {pack.Id}.");
			}
		}
	}
}
