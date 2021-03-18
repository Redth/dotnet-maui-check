using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using MauiDoctor.Doctoring;

namespace MauiDoctor.Checkups
{
	public class AndroidSdkManagerCheckup : Checkup
	{
		public AndroidSdkManagerCheckup()
		{
		}

		public override string Id => "androidsdk";

		public override string Title => "Android SDK";

		public override string[] Dependencies => new [] { "openjdk" };

		public DirectoryInfo SelectedHome { get; private set; }

		public FileInfo SdkManagerPath { get; private set; }

		public override Task<Diagonosis> Examine(PatientHistory history)
		{
			try
			{
				var homes = AndroidSdk.AndroidSdkManager.FindHome();
			
				foreach (var home in homes)
				{
					try
					{
						var sdk = new AndroidSdk.SdkManager(home);

						var v = sdk.GetVersion();

						if (v != default)
						{
							if (SelectedHome == default)
							{
								SelectedHome = home;
								SdkManagerPath = sdk.FindToolPath(SelectedHome);

								if (SdkManagerPath != null)
								{
									Util.SetDoctorEnvironmentVariable("ANDROID_SDK_ROOT", SdkManagerPath.FullName);
									Util.SetDoctorEnvironmentVariable("ANDROID_HOME", SdkManagerPath.FullName);
								}

								ReportStatus($"{home.FullName} ({v}) installed.", Status.Ok);
							}
							else
							{
								ReportStatus($"{home.FullName} ({v}) also installed.", Status.Ok);
							}
						}
						else
						{
							ReportStatus($"{home.FullName} invalid.", Status.Warning);
						}
					}
					catch
					{
						ReportStatus($"{home.FullName} invalid.", Status.Warning);
					}
				}

				if (SelectedHome != default)
					return Task.FromResult(Diagonosis.Ok(this));
			} catch { }

			return Task.FromResult(
				new Diagonosis(
					Status.Error,
					this,
					"Failed to find Android SDK.",
					new Prescription("Please Install the Android SDK Manager.  For more information see: https://aka.ms/dotnet-androidsdk-help"))); //,
				//new ActionRemedy((r, ct) =>
				//{
				//	if (SelectedHome != null)
				//	{
				//		if (SelectedHome.Exists)
				//		{
				//			try { SelectedHome.Delete(true); }
				//			catch (UnauthorizedAccessException)
				//			{
				//				throw new Exception("Fix requires running with adminstrator privileges.  Try opening a terminal as administrator and running maui-doctor again.");
				//			}
				//			catch (Exception ex)
				//			{
				//				throw new Exception("Failed to delete existing Android SDK: " + ex.Message);
				//			}

				//			try { SelectedHome.Create(); }
				//			catch { }
				//		}
				//	}
				//	else
				//	{
				//		SelectedHome = new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Android", "android-sdk"));
				//		try { SelectedHome.Create(); }
				//		catch { }
				//	}

				//	var sdk = new AndroidSdk.AndroidSdkManager(SelectedHome);

				//	sdk.Acquire();

				//	return Task.CompletedTask;
				//}))));
		}
	}
}
