using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotNetCheck.Models;
using DotNetCheck.Manifest;
using NuGet.Versioning;
using DotNetCheck.Solutions;
using Xamarin.Installer.AndroidSDK;
using Xamarin.Installer.AndroidSDK.Manager;

namespace DotNetCheck.Checkups
{
	public class AndroidEmulatorCheckup : Checkup
	{
		public override IEnumerable<CheckupDependency> DeclareDependencies(IEnumerable<string> checkupIds)
			=> new [] { new CheckupDependency("androidsdk") };

		public IEnumerable<AndroidEmulator> RequiredEmulators
			=> Manifest?.Check?.Android?.Emulators;

		public override string Id => "androidemulator";

		public override string Title => "Android Emulator";

		public override bool ShouldExamine(SharedState history)
			=> RequiredEmulators?.Any() ?? false;

		public override Task<DiagnosticResult> Examine(SharedState history)
		{
			var android = new AndroidSdk.AndroidSdkManager(
				history.GetEnvironmentVariable("ANDROID_SDK_ROOT") ?? history.GetEnvironmentVariable("ANDROID_HOME"));

			var avds = AndroidSdk.AvdManager.ListAvdsFromFiles(); // android.AvdManager.ListAvds();

			// This isn't really helpful anymore as avd cli tools don't seem to work with JDK 11
			//if (!avds.Any())
			//	avds = android.AvdManager.ListAvds();

			if (avds.Any())
			{
				var emu = avds.FirstOrDefault();

				ReportStatus($"Emulator: {emu.Name ?? emu.SdkId} found.", Status.Ok);

				return Task.FromResult(DiagnosticResult.Ok(this));
			}

			// If we got here, there are no emulators at all
			var missingEmulators = RequiredEmulators;

			if (!missingEmulators.Any())
				return Task.FromResult(DiagnosticResult.Ok(this));

			AndroidSdk.AvdManager.AvdDevice preferredDevice = null;

			try
			{
				var devices = android.AvdManager.ListDevices();

				preferredDevice = devices.FirstOrDefault(d => d.Name.Contains("pixel", StringComparison.OrdinalIgnoreCase));
			}
			catch (Exception ex)
			{
				var msg = "Unable to find any Android Emulators.  You can use Visual Studio to create one if necessary: [underline]https://docs.microsoft.com/xamarin/android/get-started/installation/android-emulator/device-manager[/]";

				ReportStatus(msg, Status.Warning);

				Util.Exception(ex);
				return Task.FromResult(
					new DiagnosticResult(Status.Warning, this, msg));
			}

			return Task.FromResult(new DiagnosticResult(
				Status.Error,
				this,
				new Suggestion("Create an Android Emulator",
					missingEmulators.Select(me =>
						new ActionSolution(t =>
						{
							try
							{
								var installer = new AndroidSDKInstaller(new Helper(), AndroidManifestType.GoogleV2);
								installer.Discover();

								var sdkInstance = installer.FindInstance(null);

								var installedPackages = sdkInstance.Components.AllInstalled(true);

								var sdkPackage = installedPackages.FirstOrDefault(p => p.Path.Equals(me.SdkId, StringComparison.OrdinalIgnoreCase));

								if (sdkPackage == null && (me.AlternateSdkIds?.Any() ?? false))
									sdkPackage = installedPackages.FirstOrDefault(p => me.AlternateSdkIds.Any(a => a.Equals(p.Path, StringComparison.OrdinalIgnoreCase)));

								var sdkId = sdkPackage?.Path ?? me.SdkId;

								android.AvdManager.Create($"Android_Emulator_{me.ApiLevel}", sdkId, device: preferredDevice?.Id, tag: "google_apis", force: true, interactive: true);
								return Task.CompletedTask;
							}
							catch (Exception ex)
							{
								ReportStatus("Unable to create Emulator.  Use Visual Studio to create one instead: https://docs.microsoft.com/xamarin/android/get-started/installation/android-emulator/device-manager", Status.Warning);
								Util.Exception(ex);
							}

							return Task.CompletedTask;
						})).ToArray())));
		}
	}

}
