using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotNetCheck.Models;
using Microsoft.NET.Sdk.WorkloadManifestReader;
using NuGet.Versioning;
using DotNetCheck.DotNet;
using DotNetCheck.Solutions;

namespace DotNetCheck.Checkups
{
	public class DotNetPacksCheckup : Checkup
	{
		public DotNetPacksCheckup(string sdkVersion, Manifest.DotNetSdkPack[] requiredPacks, params string[] nugetPackageSources) : base()
		{
			SdkVersion = sdkVersion;
			RequiredPacks = requiredPacks;
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
		public readonly Manifest.DotNetSdkPack[] RequiredPacks;

		public override IEnumerable<CheckupDependency> Dependencies
			=> new List<CheckupDependency> { new CheckupDependency("dotnetworkloads") };

		public override string Id => "dotnetpacks";

		public override string Title => $".NET Core SDK - Packs ({SdkVersion})";

		public override async Task<DiagnosticResult> Examine(SharedState history)
		{
			var sdkPacks = workloadManager.GetAllInstalledWorkloadPacks();

			var missingPacks = new List<Manifest.DotNetSdkPack>();

			var requiredPacks = new List<Manifest.DotNetSdkPack>();
			requiredPacks.AddRange(RequiredPacks);

			if (history.TryGetState<WorkloadResolver.PackInfo[]>("dotnetworkloads", "required_packs", out var p) && p.Any())
				requiredPacks.AddRange(p.Select(pi => new Manifest.DotNetSdkPack { Id = pi.Id, Version = pi.Version }));

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
				return DiagnosticResult.Ok(this);

			var remedies = missingPacks
				.Select(ms => new DotNetPackInstallSolution(SdkRoot, SdkVersion, ms, NuGetPackageSources));

			return new DiagnosticResult(
				Status.Error,
				this,
				new Suggestion("Install Missing SDK Packs",
				remedies.ToArray()));
		}
	}
}
