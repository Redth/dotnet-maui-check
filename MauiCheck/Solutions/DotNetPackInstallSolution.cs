using DotNetCheck.DotNet;
using DotNetCheck.Manifest;
using DotNetCheck.Models;
using System.Threading;
using System.Threading.Tasks;

namespace DotNetCheck.Solutions
{
	public class DotNetPackInstallSolution : Solution
	{
		public DotNetPackInstallSolution(string sdkRoot, string sdkVersion, NuGetPackage package, params string[] nugetPackageSources)
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

		public override async Task Implement(CancellationToken cancellationToken)
		{
			await base.Implement(cancellationToken);

			ReportStatus($"Installing Pack: {Package.Id}...");

			if (await WorkloadManager.InstallWorkloadPack(Package.Id, cancellationToken))
				ReportStatus($"Installed Pack: {Package.Id}.");
			else
				ReportStatus($"Failed to install Pack: {Package.Id}.");
		}
	}
}
