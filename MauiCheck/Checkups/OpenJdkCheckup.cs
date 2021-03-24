using DotNetCheck.Models;
using NuGet.Versioning;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DotNetCheck.Checkups
{
	public class OpenJdkInfo
	{
		public OpenJdkInfo(string javaCFile, NuGetVersion version)
		{
			JavaC = new FileInfo(javaCFile);
			Version = version;
		}

		public FileInfo JavaC { get; set; }

		public DirectoryInfo Directory
			=> new DirectoryInfo(Path.Combine(JavaC.Directory.FullName, ".."));

		public NuGetVersion Version { get; set; }
	}

	public class OpenJdkCheckup : Models.Checkup
	{
		public NuGetVersion MinimumVersion
			=> Extensions.ParseVersion(Manifest?.Check?.OpenJdk?.MinimumVersion, new NuGetVersion("1.8.0-1"));

		public NuGetVersion ExactVersion
			=> Extensions.ParseVersion(Manifest?.Check?.OpenJdk?.ExactVersion);

		public override string Id => "openjdk";

		public override string Title => $"OpenJDK {MinimumVersion.ThisOrExact(ExactVersion)}";

		static string PlatformJavaCExtension => Util.IsWindows ? ".exe" : string.Empty;

		public override bool ShouldExamine(SharedState history)
			=> Manifest?.Check?.OpenJdk != null;

		public override Task<DiagnosticResult> Examine(SharedState history)
		{
			var jdks = FindJdks();

			var ok = false;

			foreach (var jdk in jdks)
			{
				if ((jdk.JavaC.FullName.Contains("microsoft") || jdk.JavaC.FullName.Contains("openjdk"))
					&& jdk.Version.IsCompatible(MinimumVersion, ExactVersion))
				{
					ok = true;
					ReportStatus($"{jdk.Version} ({jdk.Directory})", Status.Ok);
					history.SetEnvironmentVariable("JAVA_HOME", jdk.Directory.FullName);
				}
				else
					ReportStatus($"{jdk.Version} ({jdk.Directory})", null);
			}

			if (ok)
				return Task.FromResult(DiagnosticResult.Ok(this));

			return Task.FromResult(new DiagnosticResult(Status.Error, this));
		}

		IEnumerable<OpenJdkInfo> FindJdks()
		{
			var paths = new List<OpenJdkInfo>();

			if (Util.IsWindows)
			{
				SearchDirectoryForJdks(paths, "C:\\Program Files\\Android\\Jdk\\", true);
			} else if (Util.IsMac)
			{
				var msDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Developer", "Xamarin", "jdk");
				SearchDirectoryForJdks(paths, msDir, true);
			}

			SearchDirectoryForJdks(paths, Environment.GetEnvironmentVariable("JAVA_HOME") ?? string.Empty, true);
			SearchDirectoryForJdks(paths, Environment.GetEnvironmentVariable("JDK_HOME") ?? string.Empty, true);

			var environmentPaths = Environment.GetEnvironmentVariable("PATH")?.Split(';') ?? Array.Empty<string>();

			foreach (var envPath in environmentPaths)
			{
				if (envPath.Contains("java", StringComparison.OrdinalIgnoreCase) || envPath.Contains("jdk", StringComparison.OrdinalIgnoreCase))
					SearchDirectoryForJdks(paths, envPath, true);
			}

			return paths
				.GroupBy(i => i.JavaC.FullName)
				.Select(g => g.First());
		}

		void SearchDirectoryForJdks(IList<OpenJdkInfo> found, string directory, bool recursive = true)
		{
			if (string.IsNullOrEmpty(directory))
				return;

			var dir = new DirectoryInfo(directory);

			if (dir.Exists)
			{
				var files = dir.EnumerateFileSystemInfos($"javac{PlatformJavaCExtension}", recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);

				foreach (var file in files)
				{
					if (TryGetJavaJdkInfo(file.FullName, out var jdkInfo))
						found.Add(jdkInfo);
				}
			}
		}

		static readonly Regex rxJavaCVersion = new Regex("[0-9\\.\\-_]+", RegexOptions.Singleline);

		bool TryGetJavaJdkInfo(string javacFilename, out OpenJdkInfo javaJdkInfo)
		{
			var r = ShellProcessRunner.Run(javacFilename, "-version");
			var m = rxJavaCVersion.Match(r.GetOutput() ?? string.Empty);

			var v = m?.Value;

			if (!string.IsNullOrEmpty(v) && NuGetVersion.TryParse(v, out var version))
			{
				javaJdkInfo = new OpenJdkInfo(javacFilename, version);
				return true;
			}

			javaJdkInfo = default;
			return false;
		}
	}
}
