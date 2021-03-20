using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MauiDoctor.Doctoring;
using Microsoft.NET.Sdk.WorkloadManifestReader;
using NuGet.Versioning;

namespace MauiDoctor.Checkups
{
	public class DotNetWorkloadsCheckup : Checkup
	{
		public DotNetWorkloadsCheckup(string sdkVersion, Manifest.DotNetWorkload[] requiredWorkloads, params string[] nugetPackageSources) : base()
		{
			SdkVersion = sdkVersion;
			RequiredWorkloads = requiredWorkloads;
			NuGetPackageSources = nugetPackageSources;

			dotnet = new DotNet();
			SdkRoot = dotnet.DotNetSdkLocation.FullName;
			workloadManager = new DotNetWorkloadManager(SdkRoot, SdkVersion, NuGetPackageSources);
		}

		readonly DotNet dotnet;
		readonly DotNetWorkloadManager workloadManager;

		public readonly string SdkRoot;
		public readonly string SdkVersion;
		public readonly string[] NuGetPackageSources;
		public readonly Manifest.DotNetWorkload[] RequiredWorkloads;

		public override IEnumerable<CheckupDependency> Dependencies
			=> new List<CheckupDependency> { new CheckupDependency("dotnet") };

		public override string Id => "dotnetworkloads";

		public override string Title => $".NET Core SDK - Workloads ({SdkVersion})";

		public override async Task<Diagonosis> Examine(PatientHistory history)
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
				history.AddNotes(this, "required_packs", requiredPacks.ToArray());

			if (!missingWorkloads.Any())
				return Diagonosis.Ok(this);

			var remedies = missingWorkloads
				.Select(mw => new DotNetWorkloadInstallRemedy(SdkRoot, SdkVersion, mw, NuGetPackageSources));

			return new Diagonosis(
				Status.Error,
				this,
				new Prescription("Install Missing SDK Workloads",
				remedies.ToArray()));
		}
	}
}
