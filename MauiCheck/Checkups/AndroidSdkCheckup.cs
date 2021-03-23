using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using DotNetCheck.Models;
using Xamarin.Installer.AndroidSDK;
using Xamarin.Installer.AndroidSDK.Common;
using Xamarin.Installer.AndroidSDK.Manager;

namespace DotNetCheck.Checkups
{
	public class XAndroidSdkPackagesCheckup : Models.Checkup
	{
		public XAndroidSdkPackagesCheckup(Manifest.AndroidPackage[] requiredPackages = null)
		{
			RequiredPackages = requiredPackages.Where(p => p.IsArchCompatible()).ToArray();
		}

		public override IEnumerable<CheckupDependency> Dependencies
			=> new List<CheckupDependency> { new CheckupDependency("openjdk") };

		public Manifest.AndroidPackage[] RequiredPackages { get; private set; }

		public override string Id => "androidsdk";

		public override string Title => "Android SDK";

		List<string> temporaryFiles = new List<string>();

		public override Task<DiagnosticResult> Examine(SharedState history)
		{
			var missingPackages = new List<IAndroidComponent>();

			var installer = new AndroidSDKInstaller(new Helper(), AndroidManifestType.GoogleV2);

			installer.Discover();

			var sdkInstance = installer.FindInstance(null);

			if (string.IsNullOrEmpty(sdkInstance?.Path))
			{
				return Task.FromResult(
				new DiagnosticResult(
					Status.Error,
					this,
					"Failed to find Android SDK.",
					new Suggestion("Install the Android SDK",
					"For more information see: [underline]https://aka.ms/dotnet-androidsdk-help[/]")));
			}

			history.SetEnvironmentVariable("ANDROID_SDK_ROOT", sdkInstance.Path);
			history.SetEnvironmentVariable("ANDROID_HOME", sdkInstance.Path);

			var allNotInstalled = sdkInstance?.Components?.AllNotInstalled();

			foreach (var package in RequiredPackages)
			{
				var v = !string.IsNullOrWhiteSpace(package.Version) ? new AndroidRevision(package.Version) : null;
				var notInstalled = allNotInstalled.
					FirstOrDefault(c => c.Path== package.Path && c.Revision == (v ?? c.Revision));

				if (notInstalled != null)
				{
					ReportStatus($"{package.Path} ({package.Version}) missing.", Status.Error);
					missingPackages.Add(notInstalled);
				}
				else
				{
					ReportStatus($"{package.Path} ({package.Version})", Status.Ok);
				}
			}

			if (!missingPackages.Any())
				return Task.FromResult(DiagnosticResult.Ok(this));


			var installationSet = installer.GetInstallationSet(sdkInstance, missingPackages);

			var desc =
@$"Your Android SDK has missing our outdated packages.
You can use the Android SDK Manager to install / update them.
For more information see: [underline]https://aka.ms/dotnet-androidsdk-help[/]";

			return Task.FromResult(new DiagnosticResult(
				Status.Error,
				this,
				new Suggestion("Install or Update Android SDK pacakges",
					desc,
					new Solutions.ActionSolution(async cancelToken =>
					{
						try
						{
							var downloads = installer.GetDownloadItems(installationSet);
							using (var httpClient = new HttpClient())
							{
								// Provide a default timeout value of 7 minutes if a value is not provided.
								httpClient.Timeout = TimeSpan.FromMinutes(120);
								await Task.WhenAll(downloads.Select(d => Download(httpClient, d)));
							}

							installer.Install(sdkInstance, installationSet);
						}
						finally
						{
							foreach (var temp in temporaryFiles)
							{
								if (File.Exists(temp))
								{
									try
									{
										File.Delete(temp);
									}
									catch { }
								}
							}
						}
					}))));
		}

		async Task Download(HttpClient httpClient, Archive archive)
		{
			ReportStatus($"Downloading {archive.Url} ...", null);

			using (var response = await httpClient.GetAsync(archive.Url, HttpCompletionOption.ResponseHeadersRead))
			{
				response.EnsureSuccessStatusCode();
				var fileLength = response.Content.Headers.ContentLength.Value;
				var path = Path.GetTempFileName();
				temporaryFiles.Add(path);
				using (var fileStream = File.OpenWrite(path))
				{
					using (var httpStream = await response.Content.ReadAsStreamAsync())
					{
						var buffer = new byte[16 * 1024];
						int bytesRead;
						double bytesWritten = 0;
						double previousProgress = 0;
						while ((bytesRead = httpStream.Read(buffer, 0, buffer.Length)) > 0)
						{
							fileStream.Write(buffer, 0, bytesRead);
							bytesWritten += bytesRead;
							// Log download progress roughly every 10%.
							var progress = bytesWritten / fileLength;
							if (progress - previousProgress > .10)
							{
								ReportStatus($"Downloaded {progress:P0} of {Path.GetFileName(archive.Url.AbsolutePath)} ...", null);
								previousProgress = progress;
							}
						}
						fileStream.Flush();
					}
				}
				ReportStatus($"Wrote '{archive.Url}' to '{path}'.", null);
				archive.DownloadedFilePath = path;
			}
		}
	}
}
