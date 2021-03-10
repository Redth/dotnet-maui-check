using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MauiDoctor.Doctoring;

namespace MauiDoctor.Checks
{
	public class AndroidSdkManagerCheckup : Checkup
	{
		public AndroidSdkManagerCheckup()
		{
		}

		public override string Id => "androidsdk";

		public override string Title => "Android SDK";

		public override Task<Diagonosis> Examine()
		{
			var android = new Android();

			try
			{
				var v = android.GetSdkManagerVersion();

				if (v != default)
					return Task.FromResult(Diagonosis.Ok(this));
			} catch { }

			return Task.FromResult(new Diagonosis(Status.Error, this, new Prescription("Install Android SDK Manager",
				new ActionRemedy((r, ct) =>
				{
					android.Acquire();
					return Task.CompletedTask;
				}))));
		}
	}
}
