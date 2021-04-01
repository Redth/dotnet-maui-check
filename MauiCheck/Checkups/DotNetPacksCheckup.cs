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
		public DotNetPacksCheckup() : base()
		{
			throw new Exception("Do not IOC this type directly");
		}

		public DotNetPacksCheckup(SharedState sharedState, string sdkVersion, Manifest.DotNetSdkPack[] requiredPacks, params string[] nugetPackageSources) : base()
		{
			var dn = new DotNetSdk(sharedState);

			SdkRoot = dn.DotNetSdkLocation.FullName;
			SdkVersion = sdkVersion;
			RequiredPacks = requiredPacks;
			NuGetPackageSources = nugetPackageSources;
		}

		public readonly string SdkRoot;
		public readonly string SdkVersion;
		public readonly string[] NuGetPackageSources;
		public readonly Manifest.DotNetSdkPack[] RequiredPacks;

		public override IEnumerable<CheckupDependency> DeclareDependencies(IEnumerable<string> checkupIds)
			=> checkupIds
				?.Where(id => id.StartsWith("dotnetworkloads"))
				?.Select(id => new CheckupDependency(id, false));

		public override string Id => "dotnetpacks-" + SdkVersion;

		public override string Title => $".NET SDK - Packs ({SdkVersion})";

		public override bool ShouldExamine(SharedState history)
			=> GetAllRequiredPacks(history)?.Any() ?? false;

		IEnumerable<Manifest.DotNetSdkPack> GetAllRequiredPacks(SharedState history)
		{
			var requiredPacks = new List<Manifest.DotNetSdkPack>();
			requiredPacks.AddRange(RequiredPacks.Where(rp => rp.IsCompatible()));

			if (history.TryGetStateFromAll<WorkloadResolver.PackInfo[]>("required_packs", out var p) && p.Any())
			{
				foreach (var packset in p)
				{
					requiredPacks.AddRange(packset
						.Select(pi => new Manifest.DotNetSdkPack { Id = pi.Id, Version = pi.Version }));
				}
			}

			return requiredPacks
				.GroupBy(p => p.Id + p.Version.ToString())
				.Select(g => g.First());
		}

		public override Task<DiagnosticResult> Examine(SharedState history)
		{
			string sdkVersion;
			if (!history.TryGetEnvironmentVariable("DOTNET_SDK_VERSION", out sdkVersion))
				sdkVersion = SdkVersion;

			var workloadManager = new DotNetWorkloadManager(SdkRoot, sdkVersion, NuGetPackageSources);

			var sdkPacks = workloadManager.GetAllInstalledWorkloadPacks();

			var missingPacks = new List<Manifest.DotNetSdkPack>();

			foreach (var rp in GetAllRequiredPacks(history))
			{
				Util.Log($"Looking for pack: {rp.Id} ({rp.Version})");

				if (!sdkPacks.Any(sp => sp.Id == rp.Id && sp.Version == rp.Version)
					&& !workloadManager.TemplateExistsOnDisk(rp.Id, rp.Version, rp.PackKind, rp.TemplateShortName)
					&& !workloadManager.PackExistsOnDisk(rp.Id, rp.Version))
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
				return Task.FromResult(DiagnosticResult.Ok(this));

			var remedies = missingPacks
				.Select(ms => new DotNetPackInstallSolution(SdkRoot, sdkVersion, ms, NuGetPackageSources));

			return Task.FromResult(new DiagnosticResult(
				Status.Error,
				this,
				new Suggestion("Install Missing SDK Packs",
				remedies.ToArray())));
		}
	}
}
