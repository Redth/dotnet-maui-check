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
	public class DotNetWorkloadsCheckup : Checkup
	{
		public DotNetWorkloadsCheckup() : base()
		{
			throw new Exception("Do not IOC this type directly");
		}

		public DotNetWorkloadsCheckup(string sdkVersion, Manifest.DotNetWorkload[] requiredWorkloads, params string[] nugetPackageSources) : base()
		{
			SdkVersion = sdkVersion;
			RequiredWorkloads = requiredWorkloads;
			NuGetPackageSources = nugetPackageSources;

			dotnet = new DotNetSdk();
			SdkRoot = dotnet.DotNetSdkLocation.FullName;
			workloadManager = new DotNetWorkloadManager(SdkRoot, SdkVersion, NuGetPackageSources);
		}

		readonly DotNetSdk dotnet;
		readonly DotNetWorkloadManager workloadManager;

		public readonly string SdkRoot;
		public readonly string SdkVersion;
		public readonly string[] NuGetPackageSources;
		public readonly Manifest.DotNetWorkload[] RequiredWorkloads;

		public override IEnumerable<CheckupDependency> DeclareDependencies(IEnumerable<string> checkupIds)
			=> new [] { new CheckupDependency("dotnet") };

		public override string Id => "dotnetworkloads-" + SdkVersion;

		public override string Title => $".NET SDK - Workloads ({SdkVersion})";

		public override async Task<DiagnosticResult> Examine(SharedState history)
		{
			var installedWorkloads = workloadManager.GetInstalledWorkloads();

			var missingWorkloads = new List<Manifest.DotNetWorkload>();

			var requiredPacks = new List<WorkloadResolver.PackInfo>();

			foreach (var rp in RequiredWorkloads)
			{
				if (!installedWorkloads.Any(sp => sp == rp.Id))
				{
					ReportStatus($"{rp.Id} not installed.", Status.Error);
					missingWorkloads.Add(rp);
				}
				else
				{
					ReportStatus($"{rp.Id} installed.", Status.Ok);

					var workloadPacks = workloadManager.GetPacksInWorkload(rp.Id);

					if (workloadPacks != null && workloadPacks.Any())
					{
						foreach (var wp in workloadPacks)
						{
							if (!(rp.IgnoredPackIds?.Any(ip => ip.Equals(wp.Id, StringComparison.OrdinalIgnoreCase)) ?? false))
								requiredPacks.Add(wp);
						}
					}
				}
			}
		
			if (requiredPacks.Any())
				history.ContributeState(this, "required_packs", requiredPacks.ToArray());

			if (!missingWorkloads.Any())
				return DiagnosticResult.Ok(this);

			var remedies = missingWorkloads
				.Select(mw => new DotNetWorkloadInstallSolution(SdkRoot, SdkVersion, mw, NuGetPackageSources));

			return new DiagnosticResult(
				Status.Error,
				this,
				new Suggestion("Install Missing SDK Workloads",
				remedies.ToArray()));
		}
	}
}
