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

		public override string Title => "Android SDK - Install Packages";

		public override Task<Diagonosis> Examine()
		{
			var android = new Android();

			
			var packages = android.GetPackages();

			var missingPackages = new List<Manifest.AndroidPackage>();

			foreach (var rp in RequiredPackages)
			{
				if (!packages.Any(p => p.Path.Equals(rp.Path, StringComparison.OrdinalIgnoreCase)
					&& NuGetVersion.Parse(p.Version) >= NuGetVersion.Parse(rp.Version)))
				{
					ReportStatus($"  :warning: {rp.Path} ({rp.Version}) not found.");
					missingPackages.Add(rp);
				}
				else
				{
					ReportStatus($"  :check_mark: [darkgreen]{rp.Path} ({rp.Version}) found.[/]");
				}
			}

			if (!missingPackages.Any())
				return Task.FromResult(Diagonosis.Ok(this));

			return Task.FromResult(new Diagonosis(
				Status.Error,
				this,
				new Prescription("Install Missing SDK Packs",
					new AndroidPackagesRemedy(android, missingPackages.ToArray()))));
		}
	}

	public class AndroidPackagesRemedy : Remedy
	{
		public AndroidPackagesRemedy(Android android, Manifest.AndroidPackage[] packages)
		{
			Android = android;
			Packages = packages;
		}

		public Android Android { get; private set; }
		public Manifest.AndroidPackage[] Packages { get; private set; }

		public override async Task Cure(CancellationToken cancellationToken)
		{
			await base.Cure(cancellationToken);

			Android.InstallPackages(Packages.Select(p => p.Path).ToArray());
		}
	}

}
