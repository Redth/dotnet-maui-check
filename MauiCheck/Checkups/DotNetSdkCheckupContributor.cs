using DotNetCheck.Manifest;
using DotNetCheck.Models;
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
					var packs = sdk?.Packs?.ToArray() ?? Array.Empty<DotNetSdkPack>();
					var pkgSrcs = sdk?.PackageSources?.ToArray() ?? Array.Empty<string>();

					string sdkVersion;
					if (!sharedState.TryGetEnvironmentVariable("DOTNET_SDK_VERSION", out sdkVersion))
						sdkVersion = sdk.Version;

					if (sdk.Workloads?.Any() ?? false)
						yield return new DotNetWorkloadsCheckup(sharedState, sdkVersion, workloads, pkgSrcs);

					// Always generate a packs check even if no packs, since the workloads may dynamically
					// discover packs required and register them with the SharedState
					yield return new DotNetPacksCheckup(sharedState, sdkVersion, packs, pkgSrcs);
				}
			}
		}
	}
}
