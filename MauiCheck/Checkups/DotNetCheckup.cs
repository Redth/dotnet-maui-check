using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DotNetCheck.DotNet;
using DotNetCheck.Models;
using DotNetCheck.Solutions;
using Microsoft.NET.Sdk.WorkloadManifestReader;
using Newtonsoft.Json.Linq;
using NuGet.Versioning;

namespace DotNetCheck.Checkups
{
	public class DotNetCheckup : Checkup
	{
		public IEnumerable<Manifest.DotNetSdk> RequiredSdks
			=> Manifest?.Check?.DotNet?.Sdks;

		public override string Id => "dotnet";

		public override string Title => $".NET SDK";

		string SdkListToString()
			=> (RequiredSdks?.Any() ?? false) ? "(" + string.Join(", ", RequiredSdks.Select(s => s.Version)) + ")" : string.Empty;

		public override bool ShouldExamine(SharedState history)
			=> RequiredSdks?.Any() ?? false;

		public override async Task<DiagnosticResult> Examine(SharedState history)
		{
			var dn = new DotNetSdk(history);

			var missingDiagnosis = new DiagnosticResult(Status.Error, this, new Suggestion(".NET SDK not installed"));

			if (!dn.Exists)
				return missingDiagnosis;

			var sdks = await dn.GetSdks();

			var missingSdks = new List<Manifest.DotNetSdk>();
			var sentinelFiles = new List<string>();

			if (RequiredSdks?.Any() ?? false)
			{
				foreach (var rs in RequiredSdks)
				{
					if (!sdks.Any(s => s.Version >= NuGetVersion.Parse(rs.Version)))
						missingSdks.Add(rs);
				}
			}

			DotNetSdkInfo bestSdk = null;

			foreach (var sdk in sdks)
			{
				var requiredSdk = RequiredSdks.FirstOrDefault(rs => sdk.Version == NuGetVersion.Parse(rs.Version));

				if (requiredSdk != null)
				{
					if (bestSdk == null || sdk.Version > bestSdk.Version)
						bestSdk = sdk;

					if (requiredSdk.EnableWorkloadResolver)
					{
						var sentinelPath = Path.Combine(sdk.Directory.FullName, "EnableWorkloadResolver.sentinel");
						sentinelFiles.Add(sentinelPath);
					}

					ReportStatus($"{sdk.Version} - {sdk.Directory}", Status.Ok);
				}
				else
					ReportStatus($"{sdk.Version} - {sdk.Directory}", null);
			}

			// Find newest compatible sdk
			if (bestSdk != null)
				history.SetEnvironmentVariable("DOTNET_SDK", bestSdk.Directory.FullName);

			// Add sentinel files that should be considered
			if (sentinelFiles.Any())
				history.ContributeState(this, "sentinel_files", sentinelFiles.ToArray());

			if (missingSdks.Any())
			{
				var str = SdkListToString();

				var msExeUrls = missingSdks
					.Where(ms => ms.Urls.ForCurrent().AbsolutePath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase));

				var bootsUrls = missingSdks
					.Where(ms => !ms.Urls.ForCurrent().AbsolutePath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase));


				var remedies = bootsUrls
					.Select(ms => new BootsSolution(ms.Urls.ForCurrent(), ".NET SDK " + ms.Version) as Solution)
					.Concat(msExeUrls
						.Select(ms => new MsInstallerSolution(ms.Urls.ForCurrent(), ".NET SDK " + ms.Version)));

				return new DiagnosticResult(Status.Error, this, $".NET SDK {str} not installed.",
						new Suggestion($"Download .NET SDK {str}",
						remedies.ToArray()));
			}

			return new DiagnosticResult(Status.Ok, this);
		}
	}
}
