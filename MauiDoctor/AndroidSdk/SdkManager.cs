using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.IO.Compression;
using System.Net.Http;
using System.Net.Http.Headers;

namespace MauiDoctor.AndroidSdk
{
	public partial class SdkManager : SdkTool
	{
		const string ANDROID_SDKMANAGER_MINIMUM_VERSION_REQUIRED = "2.1";
		const string REPOSITORY_URL_BASE = "https://dl.google.com/android/repository/";
		const string REPOSITORY_URL = REPOSITORY_URL_BASE + "repository2-1.xml";

		readonly Regex rxListDesc = new Regex("\\s+Description:\\s+(?<desc>.*?)$", RegexOptions.Compiled | RegexOptions.Singleline);
		readonly Regex rxListVers = new Regex("\\s+Version:\\s+(?<ver>.*?)$", RegexOptions.Compiled | RegexOptions.Singleline);
		readonly Regex rxListLoc = new Regex("\\s+Installed Location:\\s+(?<loc>.*?)$", RegexOptions.Compiled | RegexOptions.Singleline);

		public SdkManager()
			: this((DirectoryInfo)null, SdkChannel.Stable, false, false, null)
		{ }

		public SdkManager(string androidSdkHome = null, SdkChannel channel = SdkChannel.Stable, bool skipVersionCheck = false, bool includeObsolete = false, SdkManagerProxyOptions proxy = null)
			: this(androidSdkHome == null ? (DirectoryInfo)null : new DirectoryInfo(androidSdkHome), channel, skipVersionCheck, includeObsolete, proxy)
		{ }

		public SdkManager(DirectoryInfo androidSdkHome = null, SdkChannel channel = SdkChannel.Stable, bool skipVersionCheck = false, bool includeObsolete = false, SdkManagerProxyOptions proxy = null)
			: base(androidSdkHome)
		{
			AndroidSdkHome = androidSdkHome;
			Channel = channel;
			SkipVersionCheck = skipVersionCheck;
			IncludeObsolete = includeObsolete;
			Proxy = proxy ?? new SdkManagerProxyOptions();
		}

		public SdkManagerProxyOptions Proxy { get; set; }
		
		public SdkChannel Channel { get; set; } = SdkChannel.Stable;

		public bool SkipVersionCheck { get; set; }

		public bool IncludeObsolete { get; set; }

		internal override string SdkPackageId => "tools";

		// ANDROID_HOME/cmdline-tools/1.0/bin
		public override FileInfo FindToolPath(DirectoryInfo androidSdkHome)
		{
			var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
			var ext = isWindows ? ".bat" : string.Empty;

			var likelyPathSegments = new List<string[]>();

			var cmdlineToolsPath = new DirectoryInfo(Path.Combine(androidSdkHome.FullName, "cmdline-tools"));

			if (cmdlineToolsPath.Exists)
			{
				foreach (var dir in cmdlineToolsPath.GetDirectories())
				{
					var toolPath = new FileInfo(Path.Combine(dir.FullName, "bin", "sdkmanager" + ext));
					if (toolPath.Exists)
						likelyPathSegments.Insert(0, new[] { "cmdline-tools", dir.Name, "bin" });
				}
			}

			likelyPathSegments.Insert(0, new [] { "tools", "bin" });

			foreach (var pathSeg in likelyPathSegments)
			{
				var tool = FindTool(androidSdkHome, toolName: "sdkmanager", windowsExtension: ".bat", pathSeg);
				if (tool != null)
					return tool;
			}

			return null;
		}

		/// <summary>
		/// Downloads the Android SDK
		/// </summary>
		/// <param name="context">The context.</param>
		/// <param name="destinationDirectory">Destination directory, or ./tools/androidsdk if none is specified.</param>
		/// <param name="specificVersion">Specific version, or latest if none is specified.</param>
		internal void DownloadSdk(DirectoryInfo destinationDirectory = null, Version specificVersion = null, Action<int> progressHandler = null)
		{
			if (destinationDirectory == null)
				destinationDirectory = AndroidSdkHome;

			if (destinationDirectory == null)
				throw new DirectoryNotFoundException("Android SDK Directory Not specified.");

			if (!destinationDirectory.Exists)
				destinationDirectory.Create();

			var http = new HttpClient();
			http.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "text/html,application/xhtml+xml,application/xml");
			http.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Encoding", "gzip, deflate");
			http.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 6.2; WOW64; rv:19.0) Gecko/20100101 Firefox/19.0");
			http.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Charset", "ISO-8859-1");

			string platformStr;

			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
				platformStr = "windows";
			else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
				platformStr = "macosx";
			else
				platformStr = "linux";

			string archiveUrlPart = null;

			var cmdlinetoolsVersion = "latest";
			if (specificVersion != null)
				cmdlinetoolsVersion = specificVersion.Major + "." + specificVersion.Minor;

			try
			{
				var data = http.GetStringAsync(REPOSITORY_URL).Result;

				var xdoc = new System.Xml.XmlDocument();
				xdoc.LoadXml(data);

				var hostOsNode = xdoc.SelectSingleNode($"//remotePackage[@path='cmdline-tools;{cmdlinetoolsVersion}']/archives/archive/host-os[text() = '{platformStr}']");

				if (hostOsNode != null)
				{
					var urlNode = hostOsNode.ParentNode.SelectSingleNode("complete/url");

					if (urlNode != null)
						archiveUrlPart = urlNode.InnerText;
				}
			}
			catch
			{
			}

			if (string.IsNullOrEmpty(archiveUrlPart))
				archiveUrlPart = $"commandlinetools-{platformStr}-6858069_latest.zip";

			var sdkUrl = REPOSITORY_URL_BASE + archiveUrlPart;

			var sdkDir = new DirectoryInfo(destinationDirectory.FullName);
			if (!sdkDir.Exists)
				sdkDir.Create();

			var sdkZipFile = new FileInfo(Path.Combine(destinationDirectory.FullName, "androidsdk.zip"));

			if (!sdkZipFile.Exists)
			{

				var buffer = new byte[4096];

				var resp = http.GetAsync(sdkUrl).Result;

				resp.EnsureSuccessStatusCode();

				var contentLength = resp.Content.Headers.ContentLength;

				using (var httpStream = resp.Content.ReadAsStreamAsync().Result)
				using (var fileStream = File.Create(sdkZipFile.FullName))
				{
					var totalRead = 0;
					int prevProgress = 0;

					int read = 0;
					while ((read = httpStream.Read(buffer, 0, buffer.Length)) > 0)
					{
						fileStream.Write(buffer, 0, read);

						totalRead += read;

						int progress = (int)(((double)totalRead / (double)contentLength) * 100d);

						if (progress > prevProgress)
							progressHandler?.Invoke(progress);

						prevProgress = progress;
					}
				}
			}


			var zip = Xamarin.Tools.Zip.ZipArchive.Open(sdkZipFile.FullName, FileMode.Open, sdkDir.FullName, false);

			zip.ExtractAll(sdkDir.FullName);

			// Extracts to $HOME/cmdline-tools, move to $HOME/latest
			Directory.Move(Path.Combine(sdkDir.FullName, "cmdline-tools"), Path.Combine(sdkDir.FullName, "latest"));

			// Create the cmdline-tools again so we have a new blank dir
			Directory.CreateDirectory(Path.Combine(sdkDir.FullName, "cmdline-tools"));

			// Move into it so we end up with $HOME/cmdline-tools/latest
			Directory.Move(Path.Combine(sdkDir.FullName, "latest"), Path.Combine(sdkDir.FullName, "cmdline-tools", "latest"));

			//ZipFile.ExtractToDirectory(sdkZipFile.FullName, sdkDir.FullName);
		}

		public bool IsUpToDate()
		{
			if (SkipVersionCheck)
				return true;

			var v = GetVersion();

			var min = Version.Parse(ANDROID_SDKMANAGER_MINIMUM_VERSION_REQUIRED);

			if (v == null || v < min)
				return false;

			return true;
		}

		public Version GetVersion()
		{
			var builder = new ProcessArgumentBuilder();
			builder.Append("--version");

			BuildStandardOptions(builder);

			var p = Run(builder);

			if (p != null)
			{
				foreach (var l in p)
				{
					if (Version.TryParse(l?.Trim() ?? string.Empty, out var v))
						return v;
				}
			}

			return null;
		}

		internal void CheckSdkManagerVersion ()
		{
			if (SkipVersionCheck)
				return;
			
			if (!IsUpToDate())
				throw new NotSupportedException("Your sdkmanager is out of date.  Version " + ANDROID_SDKMANAGER_MINIMUM_VERSION_REQUIRED + " or later is required.");
		}

		public SdkManagerList List()
		{
			var result = new SdkManagerList();

			CheckSdkManagerVersion();

			//adb devices -l
			var builder = new ProcessArgumentBuilder();

			builder.Append("--list --verbose");

			BuildStandardOptions(builder);

			var p = Run(builder);

			int section = 0;

			var path = string.Empty;
			var description = string.Empty;
			var version = string.Empty;
			var location = string.Empty;

			foreach (var line in p)
			{
				if (line.StartsWith("------"))
					continue;
				
				if (line.ToLowerInvariant().Contains("installed packages:"))
				{
					section = 1;
					continue;
				}
				else if (line.ToLowerInvariant().Contains("available packages:"))
				{
					section = 2;
					continue;
				}
				else if (line.ToLowerInvariant().Contains("available updates:"))
				{
					section = 3;
					continue;
				}

				if (section >= 1 && section <= 2)
				{
					if (string.IsNullOrEmpty(path)) {

						// If we have spaces preceding the line, it's not a new item yet
						if (line.StartsWith(" "))
							continue;
						
						path = line.Trim();
						continue;
					}

					if (rxListDesc.IsMatch(line)) {
						description = rxListDesc.Match(line)?.Groups?["desc"]?.Value;
						continue;
					}

					if (rxListVers.IsMatch(line)) {
						version = rxListVers.Match(line)?.Groups?["ver"]?.Value;
						continue;
					}

					if (rxListLoc.IsMatch(line)) {
						location = rxListLoc.Match(line)?.Groups?["loc"]?.Value;
						continue;
					}

					// If we got here, we should have a good line of data
					if (section == 1)
					{
						result.InstalledPackages.Add(new InstalledSdkPackage
						{
							Path = path,
							Version = version,
							Description = description,
							Location = location
						});
					}
					else if (section == 2)
					{
						result.AvailablePackages.Add(new SdkPackage
						{
							Path = path,
							Version = version,
							Description = description
						});
					}

					path = null;
					description = null;
					version = null;
					location = null;
				}
			}

			return result;
		}

		public bool Install(params string[] packages)
			=> InstallOrUninstall(true, packages);

		public bool Uninstall(params string[] packages)
			=> InstallOrUninstall(false, packages);

		internal bool InstallOrUninstall(bool install, IEnumerable<string> packages)
		{
			CheckSdkManagerVersion();

			//adb devices -l
			var builder = new ProcessArgumentBuilder();

			if (!install)
				builder.Append("--uninstall");
			
			foreach (var pkg in packages)
				builder.AppendQuoted(pkg);

			BuildStandardOptions(builder);

			var output = RunWithAccept(builder);

			return true;
		}

		public bool RequiresLicenseAcceptance()
		{
			var sdkManager = FindToolPath(AndroidSdkHome);

			if (!(sdkManager?.Exists ?? false))
				throw new FileNotFoundException("Could not locate sdkmanager", sdkManager?.FullName);

			var requiresAcceptance = false;

			var cts = new CancellationTokenSource();
			var spr = new ShellProcessRunner(sdkManager.FullName, "--licenses", cts.Token, rx =>
			{
				if (rx.ToLowerInvariant().Contains("licenses not accepted"))
				{
					requiresAcceptance = true;
					cts.Cancel();
				}
			}, true);

			spr.WaitForExit();
			return requiresAcceptance;
		}

		public bool AcceptLicenses()
		{
			CheckSdkManagerVersion();

			//adb devices -l
			var builder = new ProcessArgumentBuilder();

			builder.Append("--licenses");

			BuildStandardOptions(builder);

			RunWithAccept(builder);

			return true;
		}

		public bool UpdateAll()
		{
			var sdkManager = FindToolPath(AndroidSdkHome);

			if (!(sdkManager?.Exists ?? false))
				throw new FileNotFoundException("Could not locate sdkmanager", sdkManager?.FullName);

			// We need to temporarily move ./tools/ to ./tools-temp/ and run sdkmanager.bat
			// from there on windows to avoid errors updating the ./tools/ folder while in use
			var moveToolsTemp = GetVersion() == null;
			
			//adb devices -l
			var builder = new ProcessArgumentBuilder();

			builder.Append("--update");

			BuildStandardOptions(builder);

			var o = RunWithAccept(builder, moveToolsTemp);

			return true;
		}

		public IEnumerable<string> Help()
		{
			//adb devices -l
			return Run(new ProcessArgumentBuilder());
		}

		//List<string> RunWithAccept(ProcessArgumentBuilder builder, bool moveToolsToTemp = false)
		//	=> RunWithAccept(builder, TimeSpan.Zero, moveToolsToTemp);

		//List<string> RunWithAccept(ProcessArgumentBuilder builder, TimeSpan timeout, bool moveToolsToTemp = false)
		//{
		//	var sdkManager = FindToolPath(AndroidSdkHome);

		//	if (!(sdkManager?.Exists ?? false))
		//		throw new FileNotFoundException("Could not locate sdkmanager", sdkManager?.FullName);

		//	var ct = new CancellationTokenSource();
		//	if (timeout != TimeSpan.Zero)
		//		ct.CancelAfter(timeout);

		//	// UGLY HACK AND DRAGONS 🐲🔥
		//	// Basically, on windows sdkmanager.bat is in tools, but updating itself
		//	// tries to delete tools and move the new one in place after it downloads
		//	// which causes issues because sdkmanager.bat is running from that folder
		//	string sdkToolsTempDir = null;
		//	var didMoveToolsToTemp = false;

		//	if (moveToolsToTemp && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		//	{
		//		// Get the actual tools dir
		//		var sdkToolsDir = Path.Combine(sdkManager.Directory.FullName, "..");
		//		sdkToolsTempDir = Path.Combine(sdkToolsDir, "..", "tools-temp");

		//		// Perform the copy
		//		CopyFilesRecursively(new DirectoryInfo(sdkToolsDir), new DirectoryInfo(sdkToolsTempDir));

		//		// Set the sdkmanager.bat path to the new temp location
		//		sdkManager = new FileInfo(Path.Combine(sdkToolsTempDir, "bin", "sdkmanager.bat"));
		//		didMoveToolsToTemp = true;
		//	}

		//	var p = new ProcessRunner(sdkManager, builder, ct.Token, true);

		//	while (!p.HasExited)
		//	{
		//		Thread.Sleep(250);

		//		try
		//		{
		//			p.StandardInputWriteLine("y");
		//		}
		//		catch { }
		//	}

		//	var r = p.WaitForExit();

		//	// If we used the ugly hack above, let's cleanup the temp copy
		//	if (didMoveToolsToTemp)
		//		Directory.Delete(sdkToolsTempDir, true);

		//	return r.StandardOutput;
		//}

		List<string> RunWithAccept(ProcessArgumentBuilder builder, bool moveToolsToTemp = false)
			=> RunWithAccept(builder, TimeSpan.Zero);

		List<string> RunWithAccept(ProcessArgumentBuilder builder, TimeSpan timeout, bool moveToolsToTemp = false)
		{
			var sdkManager = FindToolPath(AndroidSdkHome);

			if (!(sdkManager?.Exists ?? false))
				throw new FileNotFoundException("Could not locate sdkmanager", sdkManager?.FullName);

			var ct = new CancellationTokenSource();
			if (timeout != TimeSpan.Zero)
				ct.CancelAfter(timeout);

			var p = new ShellProcessRunner(sdkManager.FullName, builder.ToString(), ct.Token, null, true);

			while (!p.HasExited)
			{
				Thread.Sleep(250);

				try
				{
					p.Write("y");
				}
				catch { }
			}

			var r = p.WaitForExit();

			return r.StandardOutput.Concat(r.StandardError).ToList();
		}

		List<string> Run(ProcessArgumentBuilder builder)
		{
			var sdkManager = FindToolPath(AndroidSdkHome);

			if (!(sdkManager?.Exists ?? false))
				throw new FileNotFoundException("Could not locate sdkmanager", sdkManager?.FullName);

			var p = new ProcessRunner(sdkManager, builder);

			var r = p.WaitForExit();

			return r.StandardOutput;
		}

		void BuildStandardOptions(ProcessArgumentBuilder builder)
		{
			builder.Append("--verbose");

			if (Channel != SdkChannel.Stable)
				builder.Append("--channel=" + (int)Channel);

			if (AndroidSdkHome?.Exists ?? false)
				builder.Append($"--sdk_root=\"{AndroidSdkHome.FullName}\"");

			if (IncludeObsolete)
				builder.Append("--include_obsolete");

			if (Proxy?.NoHttps ?? false)
				builder.Append("--no_https");

			if ((Proxy?.ProxyType ?? SdkManagerProxyType.None) != SdkManagerProxyType.None)
			{
				builder.Append($"--proxy={Proxy.ProxyType.ToString().ToLower()}");

				if (!string.IsNullOrEmpty(Proxy.ProxyHost))
					builder.Append($"--proxy_host=\"{Proxy.ProxyHost}\"");

				if (Proxy.ProxyPort > 0)
					builder.Append($"--proxy_port=\"{Proxy.ProxyPort}\"");
			}
		}

		public static void Acquire(params SdkTool[] tools)
		{
			if (tools == null)
				return;

			foreach (var t in tools)
				t.Acquire();
		}

		internal void Acquire(params string[] installIds)
		{
			var sdkManagerApp = FindToolPath(AndroidSdkHome);

			// Download if it doesn't exist
			if (sdkManagerApp == null || !sdkManagerApp.Exists)
				DownloadSdk(AndroidSdkHome, null, null);

			UpdateAll();

			if (installIds?.Any() ?? false)
			{
				foreach (var id in installIds)
					Install(id);
			}
		}

		public static void CopyFilesRecursively(DirectoryInfo source, DirectoryInfo target)
		{
			foreach (DirectoryInfo dir in source.GetDirectories())
				CopyFilesRecursively(dir, target.CreateSubdirectory(dir.Name));
			foreach (FileInfo file in source.GetFiles())
				file.CopyTo(Path.Combine(target.FullName, file.Name));
		}
	}
}
