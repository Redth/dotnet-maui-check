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
	public class DotNetWorkloadsCheckup
		: Checkup
	{
		public DotNetWorkloadsCheckup() : base()
		{
			throw new Exception("Do not IOC this type directly");
		}

		public DotNetWorkloadsCheckup(SharedState sharedState, string sdkVersion, Manifest.DotNetWorkload[] requiredWorkloads, params string[] nugetPackageSources) : base()
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
			=> new[] { new CheckupDependency("dotnet") };

		public override string Id => "dotnetworkloads-" + SdkVersion;

		public override string Title => $".NET SDK - Workloads ({SdkVersion})";

		static bool wasForceRunAlready = false;

		public override async Task<DiagnosticResult> Examine(SharedState history)
		{
			if (!history.TryGetEnvironmentVariable("DOTNET_SDK_VERSION", out var sdkVersion))
				sdkVersion = SdkVersion;

			var force = history.TryGetEnvironmentVariable("DOTNET_FORCE", out var forceDotNet)
				&& (forceDotNet?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false)
				&& !wasForceRunAlready;

			// Don't allow multiple force runs, just the first
			if (force)
				wasForceRunAlready = true;

			var workloadManager = new DotNetWorkloadManager(SdkRoot, sdkVersion, NuGetPackageSources);

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

			if (!missingWorkloads.Any() && !force)
				return DiagnosticResult.Ok(this);

			var canPerform = true;

			if (Util.IsWindows)
			{
				var interactive = !history.GetEnvironmentVariableFlagSet("MAUI_CHECK_SETTINGS_NONINTERACTIVE");

				Spectre.Console.AnsiConsole.MarkupLine($"[bold red]{Icon.Bell} Managing Workload installation from the CLI is [underline]NOT recommended[/].  Instead you should install the latest Visual Studio preview to automatically get the newest release of .NET MAUI workloads installed.[/]");

				// If this is not a CI / Fix flag install, ask if we want to confirm to continue the CLI install after seeing the warning
				if (interactive)
					canPerform = Spectre.Console.AnsiConsole.Confirm("Are you sure you would like to continue the CLI workload installation?", false);
			}

			return new DiagnosticResult(
				Status.Error,
				this,
				canPerform ? 
					new Suggestion("Install or Update SDK Workloads",
						new ActionSolution(async (sln, cancel) =>
						{
							if (history.GetEnvironmentVariableFlagSet("DOTNET_FORCE"))
							{
								try
								{
									await workloadManager.Repair();
								}
								catch (Exception ex)
								{
									ReportStatus("Warning: Workload repair failed", Status.Warning);
								}
							}

							await workloadManager.Install(RequiredWorkloads);
						}))
				: new Suggestion("Install the latest Visual Studio Preview", "To install or update to the latest workloads for .NET MAUI, install the latest Visual Studio Preview and choose .NET MAUI in the list of features to install under the .NET Mobile Workload: [underline]https://visualstudio.microsoft.com/vs/preview/[/]"));
		}
	}
}
