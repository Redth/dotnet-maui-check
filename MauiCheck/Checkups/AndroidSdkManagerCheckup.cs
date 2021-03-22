using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DotNetCheck.Models;

namespace DotNetCheck.Checkups
{
	public class AndroidSdkManagerCheckup : Checkup
	{
		public AndroidSdkManagerCheckup()
		{
		}

		public override string Id => "androidsdk";

		public override string Title => "Android SDK";

		public override IEnumerable<CheckupDependency> Dependencies
			=> new List<CheckupDependency> { new CheckupDependency("openjdk") };

		public DirectoryInfo SelectedHome { get; private set; }

		public FileInfo SdkManagerPath { get; private set; }

		public override Task<DiagnosticResult> Examine(SharedState history)
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
									history.SetEnvironmentVariable("ANDROID_SDK_ROOT", SelectedHome.FullName);
									history.SetEnvironmentVariable("ANDROID_HOME", SelectedHome.FullName);
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
					return Task.FromResult(DiagnosticResult.Ok(this));
			} catch { }

			return Task.FromResult(
				new DiagnosticResult(
					Status.Error,
					this,
					"Failed to find Android SDK.",
					new Suggestion("Install the Android SDK Manager",
					"For more information see: [underline]https://aka.ms/dotnet-androidsdk-help[/]")));
		}
	}
}
