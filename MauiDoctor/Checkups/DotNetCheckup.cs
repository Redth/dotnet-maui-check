using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MauiDoctor.Doctoring;
using Microsoft.NET.Sdk.WorkloadManifestReader;
using NuGet.Versioning;

namespace MauiDoctor.Checkups
{
	public class DotNetCheckup : Checkup
	{
		public DotNetCheckup(params Manifest.DotNetSdk[] requiredSdks) : base()
		{
			RequiredSdks = requiredSdks;
		}

		public Manifest.DotNetSdk[] RequiredSdks { get; private set; }

		public override string Id => "dotnet";

		public override string Title => $".NET Core SDK";

		string SdkListToString()
			=> (RequiredSdks?.Any() ?? false) ? "(" + string.Join(", ", RequiredSdks.Select(s => s.Version)) + ")" : string.Empty;

		public override async Task<Diagonosis> Examine(PatientHistory history)
		{
			var dn = new DotNet();

			var missingDiagnosis = new Diagonosis(Status.Error, this, new Prescription(".NET SDK not installed"));

			if (!dn.Exists)
				return missingDiagnosis;

			var sdks = await dn.GetSdks();

			var missingSdks = new List<Manifest.DotNetSdk>();
			var sentinelFiles = new List<string>();

			if (RequiredSdks?.Any() ?? false)
			{
				foreach (var rs in RequiredSdks)
				{
					if (!sdks.Any(s => s.Version.Equals(NuGetVersion.Parse(rs.Version))))
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
				history.AddNotes(this, "sentinel_files", sentinelFiles.ToArray());

			if (missingSdks.Any())
			{
				var str = SdkListToString();

				var remedies = missingSdks.Select(ms =>
					new BootsRemedy(ms.Urls.For(Util.Platform).ToString(), ".NET SDK " + ms.Version)
					{
						AdminRequirements = new[] { (Platform.Windows, true) }
					});

				return new Diagonosis(Status.Error, this, $".NET SDK {str} not installed.",
						new Prescription($"Download .NET SDK {str}",
						remedies.ToArray()));
			}

			return new Diagonosis(Status.Ok, this);
		}
	}
}
