using System;
using System.Collections.Generic;
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

		public override string[] Dependencies => new[] { "androidsdk" };

		public override string Title => "Android SDK - Licenses Acceptance";

		public override Task<Diagonosis> Examine()
		{
			var android = new AndroidSdk.AndroidSdkManager();

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

			return Task.FromResult(new Diagonosis(Status.Error, this, new Prescription("Android SDK has licenses which need to be accepted.",
				$"To read and accept Android SDK licenses, run the following command in a terminal:  sdkmanager{ext} --licenses",
				new ActionRemedy((r, ct) =>
				{
					android.SdkManager.AcceptLicenses();
					return Task.CompletedTask;
				}))));
		}
	}
}
