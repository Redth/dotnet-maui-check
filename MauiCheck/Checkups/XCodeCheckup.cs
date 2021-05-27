using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DotNetCheck.Models;
using NuGet.Versioning;

namespace DotNetCheck.Checkups
{
	public class XCodeCheckup : Checkup
	{
		const string BugCommandLineToolsPath = "/Library/Developer/CommandLineTools";

		public override bool IsPlatformSupported(Platform platform)
			=> platform == Platform.OSX;

		public NuGetVersion MinimumVersion
			=> Extensions.ParseVersion(Manifest?.Check?.XCode?.MinimumVersion, new NuGetVersion("12.3"));

		public NuGetVersion ExactVersion
			=> Extensions.ParseVersion(Manifest?.Check?.XCode?.ExactVersion);

		public override string Id => "xcode";

		public override string Title => $"XCode {MinimumVersion.ThisOrExact(ExactVersion)}";

		public override bool ShouldExamine(SharedState history)
			=> Manifest?.Check?.XCode != null;

		const string DefaultXcodePath = "/Applications/Xcode.app/Contents/Developer";

		public override Task<DiagnosticResult> Examine(SharedState history)
		{
			FileInfo xcode = null;

			try
			{
				xcode = GetSelectedXCode();
			}
			catch (ArgumentException)
			{
				// If this is the case, they need to run xcode-select -s
				ReportStatus($"Invalid xcode-select path found ({BugCommandLineToolsPath})", Status.Error);

				return Task.FromResult(new DiagnosticResult(
					Status.Error,
					this,
					new Suggestion("Run `sudo xcode-select --reset`",
						new Solutions.ActionSolution(cancelToken =>
						{
							var args = $"-c 'sudo xcode-select --reset'";
							Util.Log($"{ShellProcessRunner.MacOSShell} {args}");

							var p = new ShellProcessRunner(new ShellProcessRunnerOptions(ShellProcessRunner.MacOSShell, args)
							{
								RedirectOutput = Util.Verbose
							});

							p.WaitForExit();

							return Task.CompletedTask;
						}))));
			}

			if (xcode == null)
			{
				// See if they have the default location xcode
				if (Directory.Exists(DefaultXcodePath))
				{
					var defaultXcodeInfo = GetInfo(DefaultXcodePath);

					// See if that default location has some version info and it's good enough for the required
					if (defaultXcodeInfo?.Version?.IsCompatible(MinimumVersion, ExactVersion) ?? false)
					{
						// If this is the case, they need to run xcode-select -s
						ReportStatus($"No Xcode.app is selected, but one was found ({DefaultXcodePath})", Status.Error);

						return Task.FromResult(new DiagnosticResult(
							Status.Error,
							this,
							new Suggestion("Run xcode-select -s <Path>",
								new Solutions.ActionSolution(cancelToken =>
								{
									ShellProcessRunner.Run("xcode-select", "-s " + DefaultXcodePath);
									return Task.CompletedTask;
								}))));
					}
				}
			}
			else
			{
				// Get info with no default path
				var info = GetInfo(xcode.FullName);

				if (info?.Version?.IsCompatible(MinimumVersion, ExactVersion) ?? false)
				{
					ReportStatus($"XCode.app ({info.Version} {info.Build})", Status.Ok);
					return Task.FromResult(DiagnosticResult.Ok(this));
				}
			}

			ReportStatus($"XCode.app ({MinimumVersion}) not installed.", Status.Error);

			return Task.FromResult(new DiagnosticResult(
				Status.Error,
				this,
				new Suggestion($"Download XCode {MinimumVersion.ThisOrExact(ExactVersion)}")));
		}

		FileInfo GetSelectedXCode()
		{
			var r = ShellProcessRunner.Run("xcode-select", "-p");

			var path = r.GetOutput().Trim();

			if (!string.IsNullOrEmpty(path))
			{
				if (path.Equals(BugCommandLineToolsPath))
					throw new ArgumentException();

				var dir = new DirectoryInfo(path);

				if (dir.Exists)
				{
					var defXcodeBuildLoc = new FileInfo(Path.Combine(dir.FullName, "usr", "bin", "xcodebuild"));

					if (defXcodeBuildLoc.Exists)
						return defXcodeBuildLoc;

					var xcbFiles = dir.GetFiles("xcodebuild", SearchOption.AllDirectories);

					if (xcbFiles?.Any() ?? false)
						return xcbFiles.FirstOrDefault();
				}
			}

			return null;
		}

		XCodeInfo GetInfo(string path)
		{
			//Xcode 12.4
			//Build version 12D4e
			var r = ShellProcessRunner.Run(path ?? "xcodebuild", "-version");

			var info = new XCodeInfo();

			foreach (var line in r.StandardOutput)
			{
				if (line.StartsWith("Xcode"))
				{
					var vstr = line.Substring(5).Trim();
					if (NuGetVersion.TryParse(vstr, out var v))
						info.Version = v;
				}
				else if (line.StartsWith("Build version"))
				{
					info.Build = line.Substring(13)?.Trim();
				}
			}

			return info;
		}

		
	}

	public class XCodeInfo
	{
		public NuGetVersion Version { get; set; }
		public string Build { get; set; }
	}
}
