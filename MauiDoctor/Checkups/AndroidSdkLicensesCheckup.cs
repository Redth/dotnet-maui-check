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

		public override string[] Dependencies => new[] { "androidsdkpackages" };

		public override string Title => "Android SDK - Accept Licenses";

		public override async Task<Diagonosis> Examine()
		{
			var android = new Android();

			try
			{
				var v = await android.RequiresLicenseAcceptance();

				if (!v)
				{
					ReportStatus($":check_mark: [bold darkgreen]All licenses accepted.[/]");
					return Diagonosis.Ok(this);
				}
			}
			catch { }

			return new Diagonosis(Status.Error, this, new Prescription("Accept Licenses in Android SDK Manager",
				new ActionRemedy(async (r, ct) =>
				{
					await android.AcceptLicenses();
				})));
		}
	}
}
