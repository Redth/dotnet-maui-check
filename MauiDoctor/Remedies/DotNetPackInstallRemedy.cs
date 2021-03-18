using MauiDoctor.Manifest;
using System.Threading;
using System.Threading.Tasks;

namespace MauiDoctor.Doctoring
{
	public class DotNetPackInstallRemedy : Remedy
	{
		public DotNetPackInstallRemedy(string sdkRoot, string sdkVersion, NuGetPackage[] packages, params string[] nugetPackageSources)
		{
			Packages = packages;
			WorkloadManager = new DotNetWorkloadManager(sdkRoot, sdkVersion, nugetPackageSources);
		}

		public readonly DotNetWorkloadManager WorkloadManager;
		public NuGetPackage[] Packages { get; private set; }

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
				ReportStatus($"Installing Pack: {pack.Id}...");

				if (await WorkloadManager.InstallWorkloadPack(pack.Id, cancellationToken))
					ReportStatus($"Installed Pack: {pack.Id}.");
				else
					ReportStatus($"Failed to install Pack: {pack.Id}.");
			}
		}
	}
}
