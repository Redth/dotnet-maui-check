using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MauiDoctor.Doctoring;
using Microsoft.NET.Sdk.WorkloadManifestReader;
using NuGet.Versioning;

namespace MauiDoctor.Checks
{
	public class DotNetCheckup : Checkup
	{
		public DotNetCheckup(params Manifest.DotNetSdk[] requiredSdks) : base()
		{
			RequiredSdks = requiredSdks;
		}

		public Manifest.DotNetSdk[] RequiredSdks { get; private set; }

		public override string Id => "dotnet";

		public override string Title => $".NET Core SDK " + SdkListToString();

		string SdkListToString()
			=> (RequiredSdks?.Any() ?? false) ? "(" + string.Join(", ", RequiredSdks.Select(s => s.Version)) + ")" : string.Empty;

		public override async Task<Diagonosis> Examine()
		{
			var dn = new DotNet();

			var missingDiagnosis = new Diagonosis(Status.Error, this, new Prescription(".NET SDK not found"));

			if (!dn.Exists)
				return missingDiagnosis;

			var sdks = await dn.GetSdks();

			var missingSdks = new List<Manifest.DotNetSdk>();

			if (RequiredSdks?.Any() ?? false)
			{
				foreach (var rs in RequiredSdks)
				{
					if (!sdks.Any(s => s.Version.Equals(NuGetVersion.Parse(rs.Version))))
						missingSdks.Add(rs);
				}
			}

			foreach (var sdk in sdks)
			{
				if (RequiredSdks.Any(rs => sdk.Version == NuGetVersion.Parse(rs.Version)))
					ReportStatus("  :check_mark: [darkgreen]" + sdk.Version + " - " + sdk.Directory + "[/]");
				else
					ReportStatus("  - [grey]" + sdk.Version + " - " + sdk.Directory + "[/]");
			}

			if (missingSdks.Any())
			{
				var str = SdkListToString();

				return new Diagonosis(Status.Error, this, $".NET SDK {str} not found.",
						new Prescription($"Download .NET SDK {str}",
						new BootsRemedy(missingSdks.Select(ms => (ms.Urls.For(Util.Platform)?.ToString(), ".NET SDK " + ms.Version)).ToArray())));
			}

			return new Diagonosis(Status.Ok, this);
		}
	}
}
