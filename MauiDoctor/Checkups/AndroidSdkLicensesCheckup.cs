using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using MauiDoctor.Doctoring;

namespace MauiDoctor.Checkups
{
	public class AndroidSdkLicensesCheckup : Checkup
	{
		public AndroidSdkLicensesCheckup()
		{
		}

		public override string Id => "androidsdklicenses";

		public override IEnumerable<CheckupDependency> Dependencies
			=> new List<CheckupDependency> { new CheckupDependency("androidsdk") };

		public override string Title => "Android SDK - Licenses Acceptance";

		public FileInfo SdkManagerPath { get; private set; }

		public override Task<Diagonosis> Examine(PatientHistory history)
		{
			var android = new AndroidSdk.AndroidSdkManager(
				Util.GetDoctorEnvironmentVariable("ANDROID_SDK_ROOT") ?? Util.GetDoctorEnvironmentVariable("ANDROID_HOME"));

			try
			{
				var v = android.SdkManager.RequiresLicenseAcceptance();

				if (!v)
				{
					ReportStatus($"All licenses accepted.", Status.Ok);
					return Task.FromResult(Diagonosis.Ok(this));
				}
			}
			catch { }

			ReportStatus("One or more Licenses are not accepted.", Status.Error);

			var ext = Util.IsWindows ? ".bat" : string.Empty;

			var sdkMgrPath = android.SdkManager.FindToolPath(android.Home)?.FullName;

			if (string.IsNullOrEmpty(sdkMgrPath))
				sdkMgrPath = $"sdkmanager{ext}";

			return Task.FromResult(new Diagonosis(Status.Error, this, new Prescription("Android SDK has licenses which need to be accepted.",
				$"To read and accept Android SDK licenses, run the following command in a terminal:{Environment.NewLine}    {sdkMgrPath} --licenses")));

				//,new ActionRemedy((r, ct) =>
				//{
				//	android.SdkManager.AcceptLicenses();
				//	return Task.CompletedTask;
				//}))));
		}
	}
}
