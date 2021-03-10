using System;
using System.Collections.Generic;
using System.IO;
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

		public DirectoryInfo SelectedHome { get; private set; }

		public override Task<Diagonosis> Examine()
		{
			var android = new Android();

			try
			{
				var homes = android.GetHomes();

				foreach (var home in homes)
					ReportStatus("   - Found: " + home.FullName);

				SelectedHome = homes[0];

				var v = android.GetSdkManagerVersion();

				if (v != default)
					return Task.FromResult(Diagonosis.Ok(this));
			} catch { }

			return Task.FromResult(new Diagonosis(Status.Error, this, new Prescription("Install Android SDK Manager",
				new ActionRemedy((r, ct) =>
				{
					if (SelectedHome != null)
					{
						if (SelectedHome.Exists)
						{
							try { SelectedHome.Delete(true); }
							catch (Exception ex)
							{
								throw new Exception("Failed to delete existing Android SDK: " + ex.Message);
							}

							try { SelectedHome.Create(); }
							catch { }
						}
					}
					else
					{
						SelectedHome = new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Android", "android-sdk"));
						try { SelectedHome.Create(); }
						catch { }
					}

					var sdk = new AndroidSdk.AndroidSdkManager(SelectedHome);

					sdk.Acquire();

					return Task.CompletedTask;
				}))));
		}
	}
}
