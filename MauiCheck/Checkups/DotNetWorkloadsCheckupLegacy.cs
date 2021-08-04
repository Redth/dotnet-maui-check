using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotNetCheck.DotNet;
using DotNetCheck.Models;
using DotNetCheck.Solutions;
using Microsoft.NET.Sdk.WorkloadManifestReader;
using NuGet.Versioning;

namespace DotNetCheck.Checkups
{
	public class DotNetWorkloadsCheckupLegacy : Checkup
	{
		public DotNetWorkloadsCheckupLegacy() : base()
		{
			throw new Exception("Do not IOC this type directly");
		}

		public DotNetWorkloadsCheckupLegacy(SharedState sharedState, string sdkVersion, Manifest.DotNetWorkload[] requiredWorkloads, params string[] nugetPackageSources) : base()
		{
			var dotnet = new DotNetSdk(sharedState);

			SdkRoot = dotnet.DotNetSdkLocation.FullName;
			SdkVersion = sdkVersion;
			RequiredWorkloads = requiredWorkloads;
			NuGetPackageSources = nugetPackageSources;
		}

		public readonly string SdkRoot;
		public readonly string SdkVersion;
		public readonly string[] NuGetPackageSources;
		public readonly Manifest.DotNetWorkload[] RequiredWorkloads;

		public override IEnumerable<CheckupDependency> DeclareDependencies(IEnumerable<string> checkupIds)
			=> new [] { new CheckupDependency("dotnet") };

		public override string Id => "dotnetworkloads-" + SdkVersion;

		public override string Title => $".NET SDK - Workloads ({SdkVersion})";

		public override Task<DiagnosticResult> Examine(SharedState history)
		{
			if (!history.TryGetEnvironmentVariable("DOTNET_SDK_VERSION", out var sdkVersion))
				sdkVersion = SdkVersion;

			var workloadManager = new DotNetWorkloadManagerLegacy(SdkRoot, sdkVersion, NuGetPackageSources);

			//var installedWorkloads = workloadManager.GetInstalledWorkloads();

			// This is a bit of a hack where we manually check the sdk-manifests/{SDK_VERSION}/* folders
			// for installed workloads, and then go manually parse the manifest json
			// as well as look for a .nuspec file from the extracted nupkg when it was installed
			// the nuspec file contains the version we actually care about for now since the manifest json
			// has a long number which is meaningless right now and will eventually be changed to a string
			// when that happens we can use the actual resolver's method to get installed workload info
			var installedPackageWorkloads = workloadManager.GetInstalledWorkloads();

			var missingWorkloads = new List<Manifest.DotNetWorkload>();

			foreach (var rp in RequiredWorkloads)
			{
				if (!NuGetVersion.TryParse(rp.Version, out var rpVersion))
					rpVersion = new NuGetVersion(0, 0, 0);

				// TODO: Eventually check actual workload resolver api for installed workloads and
				// compare the manifest version once it has a string in it
				if (!installedPackageWorkloads.Any(ip => ip.id.Equals(rp.Id, StringComparison.OrdinalIgnoreCase) && NuGetVersion.TryParse(ip.version, out var ipVersion) && ipVersion == rpVersion))
				{
					ReportStatus($"{rp.Id} ({rp.PackageId} : {rp.Version}) not installed.", Status.Error);
					missingWorkloads.Add(rp);
				}
				else
				{
					ReportStatus($"{rp.Id} ({rp.PackageId} : {rp.Version}) installed.", Status.Ok);
				}
			}
		
			if (!missingWorkloads.Any())
				return Task.FromResult(DiagnosticResult.Ok(this));

			return Task.FromResult(new DiagnosticResult(
				Status.Error,
				this,
				new Suggestion("Install Missing SDK Workloads",
				new DotNetWorkloadInstallSolution(SdkRoot, sdkVersion, missingWorkloads.ToArray(), NuGetPackageSources))));
		}
	}
}
