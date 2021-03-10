using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MauiDoctor.AndroidSdk;

namespace MauiDoctor
{
	public class Android
	{
		AndroidSdkManager sdkManager;

		public Android()
		{
			sdkManager = new AndroidSdkManager();

		}

		public DirectoryInfo[] GetHomes()
			=> AndroidSdkManager.FindHome().Distinct().ToArray();

		public Task<bool> AcceptLicenses()
			=> Task.FromResult(sdkManager.SdkManager.AcceptLicenses());

		public async Task<bool> RequiresLicenseAcceptance()
		{
			var sdkManagerExe = sdkManager.SdkManager.FindToolPath(sdkManager.SdkManager.AndroidSdkHome);

			var requiresAcceptance = false;

			var cts = new CancellationTokenSource();
			var spr = new ShellProcessRunner(sdkManagerExe.FullName, "--licenses", cts.Token, rx =>
			{
				if (rx.ToLowerInvariant().Contains("licenses not accepted"))
				{
					requiresAcceptance = true;
					cts.Cancel();
				}
			});

			await spr.WaitForExitAsync();

			return requiresAcceptance;
		}

		public void Acquire()
		{
			sdkManager.Acquire();
		}

		public Version GetSdkManagerVersion()
		{
			return sdkManager.SdkManager.GetVersion();
		}

		public IEnumerable<SdkPackage> GetPackages()
		{
			var packages = sdkManager.SdkManager.List();

			return packages.InstalledPackages;
		}

		public bool InstallPackages(string[] packages)
		{
			return sdkManager.SdkManager.Install(packages);
		}

		//public static Task<CheckupResult> CheckSdk()
		//{

		//}
	}
}
