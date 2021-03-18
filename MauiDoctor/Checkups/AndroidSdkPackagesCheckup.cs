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

		public override string[] Dependencies => new string[] { "androidsdk" };

		public Manifest.AndroidPackage[] RequiredPackages { get; private set; }

		public override string Id => "androidsdkpackages";

		public override string Title => "Android SDK - Installed Packages";

		public override Task<Diagonosis> Examine(PatientHistory history)
		{
			var android = new AndroidSdk.AndroidSdkManager(
				Util.GetDoctorEnvironmentVariable("ANDROID_SDK_ROOT") ?? Util.GetDoctorEnvironmentVariable("ANDROID_HOME"));

			var packages = android.SdkManager.List().InstalledPackages;

			var missingPackages = new List<Manifest.AndroidPackage>();

			foreach (var rp in RequiredPackages)
			{
				if (!packages.Any(p => p.Path.Equals(rp.Path, StringComparison.OrdinalIgnoreCase)
					&& NuGetVersion.Parse(p.Version) >= NuGetVersion.Parse(rp.Version)))
				{
					ReportStatus($"{rp.Path} ({rp.Version}) missing.", Status.Error);
					missingPackages.Add(rp);
				}
				else
				{
					ReportStatus($"{rp.Path} ({rp.Version})", Status.Ok);
				}
			}

			if (!missingPackages.Any())
				return Task.FromResult(Diagonosis.Ok(this));

			var remedies = Util.IsMac ? new AndroidPackagesRemedy[] { new AndroidPackagesRemedy(android, missingPackages.ToArray()) } : null;

			return Task.FromResult(new Diagonosis(
				Status.Error,
				this,
				new Prescription("Install missing Android SDK items",
					"Your Android SDK is missing some required packages.  You can use the Android SDK Manager to install them. For more information see: https://aka.ms/dotnet-androidsdk-help",
					remedies)));
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
