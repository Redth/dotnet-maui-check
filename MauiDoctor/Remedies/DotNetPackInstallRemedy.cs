using MauiDoctor.Manifest;
using System.Threading;
using System.Threading.Tasks;

namespace MauiDoctor.Doctoring
{
	public class DotNetPackInstallRemedy : Remedy
	{
		public DotNetPackInstallRemedy(string sdkRoot, string sdkVersion, NuGetPackage package, params string[] nugetPackageSources)
		{
			Package = package;
			WorkloadManager = new DotNetWorkloadManager(sdkRoot, sdkVersion, nugetPackageSources);
		}

		public readonly DotNetWorkloadManager WorkloadManager;
		public NuGetPackage Package { get; private set; }

		public override bool RequiresAdmin(Platform platform)
		{
			if (platform == Platform.Windows)
				return true;

			return base.RequiresAdmin(platform);
		}

		public override async Task Cure(CancellationToken cancellationToken)
		{
			await base.Cure(cancellationToken);

			ReportStatus($"Installing Pack: {Package.Id}...");

			if (await WorkloadManager.InstallWorkloadPack(Package.Id, cancellationToken))
				ReportStatus($"Installed Pack: {Package.Id}.");
			else
				ReportStatus($"Failed to install Pack: {Package.Id}.");
		}
	}
}
