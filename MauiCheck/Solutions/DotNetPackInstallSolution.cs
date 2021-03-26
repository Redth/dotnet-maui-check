using DotNetCheck.DotNet;
using DotNetCheck.Manifest;
using DotNetCheck.Models;
using System.Threading;
using System.Threading.Tasks;

namespace DotNetCheck.Solutions
{
	public class DotNetPackInstallSolution : Solution
	{
		public DotNetPackInstallSolution(string sdkRoot, string sdkVersion, DotNetSdkPack package, params string[] nugetPackageSources)
		{
			Package = package;
			SdkRoot = sdkRoot;
			WorkloadManager = new DotNetWorkloadManager(sdkRoot, sdkVersion, nugetPackageSources);
		}

		public readonly DotNetWorkloadManager WorkloadManager;
		public DotNetSdkPack Package { get; private set; }
		public readonly string SdkRoot;

		public override async Task Implement(CancellationToken cancellationToken)
		{
			await base.Implement(cancellationToken);

			ReportStatus($"Installing Pack: {Package.Id}...");

			if (await WorkloadManager.InstallWorkloadPack(SdkRoot, Package, cancellationToken))
				ReportStatus($"Installed Pack: {Package.Id}.");
			else
				ReportStatus($"Failed to install Pack: {Package.Id}.");
		}
	}
}
