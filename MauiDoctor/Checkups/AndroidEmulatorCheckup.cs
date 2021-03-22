using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MauiDoctor.Doctoring;
using MauiDoctor.Manifest;
using NuGet.Versioning;

namespace MauiDoctor.Checkups
{
	public class AndroidEmulatorCheckup : Checkup
	{
		public AndroidEmulatorCheckup(params AndroidEmulator[] emulators)
		{
			RequiredEmulators = emulators;
		}

		public override IEnumerable<CheckupDependency> Dependencies
			=> new List<CheckupDependency> { new CheckupDependency("androidsdk"), new CheckupDependency("androidsdkpackages") };

		public Manifest.AndroidEmulator[] RequiredEmulators { get; private set; }

		public override string Id => "androidemulator";

		public override string Title => "Android SDK - Emulator Created";

		public override Task<Diagonosis> Examine(PatientHistory history)
		{
			var android = new AndroidSdk.AndroidSdkManager(
				history.GetEnvironmentVariable("ANDROID_SDK_ROOT") ?? history.GetEnvironmentVariable("ANDROID_HOME"));

			var avds = android.AvdManager.ListAvds();

			var missingEmulators = new List<AndroidEmulator>();

			foreach (var emu in RequiredEmulators)
			{
				var existingAvd = avds.FirstOrDefault(a => a.SdkId.Equals(emu.SdkId, StringComparison.OrdinalIgnoreCase));

				if (existingAvd == null)
				{
					missingEmulators.Add(emu);
					ReportStatus($"{emu.Description ?? emu.SdkId} not created.", Status.Error);
				}
				else
				{
					ReportStatus($"{emu.Description ?? emu.SdkId} exists.", Status.Ok);
				}

			}

			if (!missingEmulators.Any())
				return Task.FromResult(Diagonosis.Ok(this));

			return Task.FromResult(new Diagonosis(
				Status.Error,
				this,
				new Prescription("Create an Android Emulator",
					missingEmulators.Select(me =>
						new ActionRemedy(async t =>
						{
							android.AvdManager.Create($"Android_Emulator_{me.ApiLevel}", me.SdkId, interactive: true);
						})).ToArray())));
		}
	}

}
