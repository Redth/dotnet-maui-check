using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DotNetCheck.Models;

namespace DotNetCheck.Solutions
{
	public class AndroidPackagesSolution : Solution
	{
		public AndroidPackagesSolution(AndroidSdk.AndroidSdkManager android, Manifest.AndroidPackage[] packages)
		{
			Android = android;
			Packages = packages;
		}

		public AndroidSdk.AndroidSdkManager Android { get; private set; }
		public Manifest.AndroidPackage[] Packages { get; private set; }

		public override async Task Implement(CancellationToken cancellationToken)
		{
			await base.Implement(cancellationToken);

			Android.SdkManager.InstallInteractive(Packages.Select(p => p.Path).ToArray());
		}
	}

}
