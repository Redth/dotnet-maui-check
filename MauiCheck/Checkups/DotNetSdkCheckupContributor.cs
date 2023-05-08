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
					var pkgSrcs = sdk?.PackageSources?.ToArray() ?? Array.Empty<string>();

					string sdkVersion;
					if (!sharedState.TryGetEnvironmentVariable("DOTNET_SDK_VERSION", out sdkVersion))
						sdkVersion = sdk.Version;

					if (sdk.WorkloadRollback is not null)
					{
						var workloadIds = sdk.WorkloadIds ?? new List<string> { "maui" };

						yield return new DotNetWorkloadsCheckup(sharedState, sdkVersion, sdk.WorkloadRollback, workloadIds.ToArray(), pkgSrcs);
					}
				}
			}
		}
	}
}
