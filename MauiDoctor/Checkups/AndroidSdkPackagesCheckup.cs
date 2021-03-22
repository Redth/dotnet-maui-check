using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MauiDoctor.Doctoring;
using NuGet.Versioning;

namespace MauiDoctor.Checkups
{
	public class AndroidSdkPackagesCheckup : Checkup
	{
		public AndroidSdkPackagesCheckup(Manifest.AndroidPackage[] requiredPackages = null)
		{
			RequiredPackages = requiredPackages;
		}

		public override IEnumerable<CheckupDependency> Dependencies
			=> new List<CheckupDependency> { new CheckupDependency("androidsdk") };

		public Manifest.AndroidPackage[] RequiredPackages { get; private set; }

		public override string Id => "androidsdkpackages";

		public override string Title => "Android SDK - Installed Packages";

		public override Task<Diagonosis> Examine(PatientHistory history)
		{
			var android = new AndroidSdk.AndroidSdkManager(
				history.GetEnvironmentVariable("ANDROID_SDK_ROOT") ?? history.GetEnvironmentVariable("ANDROID_HOME"));

			var packages = android.SdkManager.List().InstalledPackages;

			var missingPackages = new List<Manifest.AndroidPackage>();

			foreach (var rp in RequiredPackages)
			{
				var package = packages.FirstOrDefault(p => p.Path.Equals(rp.Path, StringComparison.OrdinalIgnoreCase));

				if (package != null)
				{
					if (NuGetVersion.Parse(package.Version) >= NuGetVersion.Parse(rp.Version))
					{
						ReportStatus($"{rp.Path} ({rp.Version})", Status.Ok);
					}
					else
					{
						ReportStatus($"{rp.Path} ({package.Version}) needs updating to {rp.Version}.", Status.Error);
						missingPackages.Add(rp);
					}
				}
				else
				{
					ReportStatus($"{rp.Path} ({rp.Version}) missing.", Status.Error);
					missingPackages.Add(rp);
				}

			}

			if (!missingPackages.Any())
				return Task.FromResult(Diagonosis.Ok(this));

			//var remedies = Util.IsMac ? new AndroidPackagesRemedy[] { new AndroidPackagesRemedy(android, missingPackages.ToArray()) } : null;


			var cmdLine = Util.IsWindows ? "^" : "\\";
			var ext = Util.IsWindows ? ".bat" : string.Empty;
			var termDesc = Util.IsWindows ? "Console" : "Terminal";

			var sdkMgrPath = android.SdkManager.FindToolPath(android.Home)?.FullName;


			if (string.IsNullOrEmpty(sdkMgrPath))
				sdkMgrPath = $"sdkmanager{ext}";

			var term = $"{sdkMgrPath} install";

			var pkgs = string.Join($" {cmdLine}{Environment.NewLine}  ", missingPackages.Select(mp => $"\"{mp.Path}\""));

			var desc =
@$"Your Android SDK has missing our outdated packages.
You can use the Android SDK Manager to install / update them.
For more information see: [underline]https://aka.ms/dotnet-androidsdk-help[/]";
//You can also try running the following {termDesc} commands:

//{sdkMgrPath} {cmdLine}
//  {pkgs}
//";


			return Task.FromResult(new Diagonosis(
				Status.Error,
				this,
				new Prescription("Install or Update Android SDK pacakges",
					desc)));
		}
	}

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

			Android.SdkManager.Install(Packages.Select(p => p.Path).ToArray());
		}
	}

}
