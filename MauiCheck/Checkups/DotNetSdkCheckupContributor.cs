using DotNetCheck.Manifest;
using DotNetCheck.Models;
using NuGet.Versioning;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DotNetCheck.Checkups
{
	public class DotNetSdkCheckupContributor : CheckupContributor
	{
		public override IEnumerable<Checkup> Contribute(Manifest.Manifest manifest, SharedState sharedState)
		{
			var sdks = manifest?.Check?.DotNet?.Sdks;

			if (sdks?.Any() ?? false)
			{
				foreach (var sdk in sdks)
				{
					var workloads = sdk?.Workloads?.ToArray() ?? Array.Empty<DotNetWorkload>();
					var pkgSrcs = sdk?.PackageSources?.ToArray() ?? Array.Empty<string>();

					string sdkVersion;
					if (!sharedState.TryGetEnvironmentVariable("DOTNET_SDK_VERSION", out sdkVersion))
						sdkVersion = sdk.Version;

					if (sdk.Workloads?.Any() ?? false)
					{
						if (NuGetVersion.Parse(sdkVersion) >= Manifest.DotNetSdk.Version6Preview7)
							yield return new DotNetWorkloadsCheckup(sharedState, sdkVersion, workloads, pkgSrcs);
						else
							yield return new DotNetWorkloadsCheckupLegacy(sharedState, sdkVersion, workloads, pkgSrcs);
					}
				}
			}
		}
	}
}
