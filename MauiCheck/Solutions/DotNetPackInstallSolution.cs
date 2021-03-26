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
			SdkVersion = sdkVersion;
			NuGetPackageSources = nugetPackageSources;
		}

		public DotNetSdkPack Package { get; private set; }
		public readonly string SdkRoot;
		public readonly string SdkVersion;
		public readonly string[] NuGetPackageSources;

		public override async Task Implement(CancellationToken cancellationToken)
		{
			await base.Implement(cancellationToken);

			ReportStatus($"Installing Pack: {Package.Id}...");

			var workloadManager = new DotNetWorkloadManager(SdkRoot, SdkVersion, NuGetPackageSources);

			if (await workloadManager.InstallWorkloadPack(SdkRoot, Package, cancellationToken))
				ReportStatus($"Installed Pack: {Package.Id}.");
			else
				ReportStatus($"Failed to install Pack: {Package.Id}.");
		}
	}
}
