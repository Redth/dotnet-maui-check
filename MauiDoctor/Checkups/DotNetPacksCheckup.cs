using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MauiDoctor.Doctoring;
using MauiDoctor.Manifest;
using Microsoft.NET.Sdk.WorkloadManifestReader;
using NuGet.Versioning;

namespace MauiDoctor.Checkups
{
	public class DotNetPacksCheckup : Checkup
	{
		public DotNetPacksCheckup(string sdkVersion, Manifest.NuGetPackage[] requiredPacks, params string[] nugetPackageSources) : base()
		{
			SdkVersion = sdkVersion;
			RequiredPacks = requiredPacks;
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
		public readonly Manifest.NuGetPackage[] RequiredPacks;

		public override IEnumerable<CheckupDependency> Dependencies
			=> new List<CheckupDependency> { new CheckupDependency("dotnetworkloads") };

		public override string Id => "dotnetpacks";

		public override string Title => $".NET Core SDK - Packs ({SdkVersion})";

		public override async Task<Diagonosis> Examine(PatientHistory history)
		{
			var sdkPacks = workloadManager.GetAllInstalledWorkloadPacks();

			var missingPacks = new List<NuGetPackage>();

			var requiredPacks = new List<NuGetPackage>();
			requiredPacks.AddRange(RequiredPacks);

			if (history.TryGetNotes<WorkloadResolver.PackInfo[]>("dotnetworkloads", "required_packs", out var p) && p.Any())
				requiredPacks.AddRange(p.Select(pi => new NuGetPackage { Id = pi.Id, Version = pi.Version }));

			var uniqueRequiredPacks = requiredPacks
				.GroupBy(p => p.Id + p.Version.ToString())
				.Select(g => g.First());

			foreach (var rp in uniqueRequiredPacks)
			{
				if (!sdkPacks.Any(sp => sp.Id == rp.Id && sp.Version == rp.Version))
				{
					ReportStatus($"{rp.Id} ({rp.Version}) not installed.", Status.Warning);
					missingPacks.Add(rp);
				}
				else
				{
					ReportStatus($"{rp.Id} ({rp.Version}) installed.", Status.Ok);
				}
			}

			if (!missingPacks.Any())
				return Diagonosis.Ok(this);

			var remedies = missingPacks
				.Select(ms => new DotNetPackInstallRemedy(SdkRoot, SdkVersion, ms, NuGetPackageSources));

			return new Diagonosis(
				Status.Error,
				this,
				new Prescription("Install Missing SDK Packs",
				remedies.ToArray()));
		}
	}
}
