using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MauiDoctor.Doctoring;

namespace MauiDoctor.Checkups
{
	public class AndroidPackagesRemedy : Remedy
	{
		public AndroidPackagesRemedy(AndroidSdk.AndroidSdkManager android, Manifest.AndroidPackage[] packages)
		{
			Android = android;
			Packages = packages;
		}

		public AndroidSdk.AndroidSdkManager Android { get; private set; }
		public Manifest.AndroidPackage[] Packages { get; private set; }

		public override async Task Cure(CancellationToken cancellationToken)
		{
			await base.Cure(cancellationToken);

			Android.SdkManager.InstallInteractive(Packages.Select(p => p.Path).ToArray());
		}
	}

}
