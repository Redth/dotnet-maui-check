using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using DotNetCheck.Models;
using NuGet.Versioning;

namespace DotNetCheck.Checkups
{
	public class VisualStudioWindowsCheckup : Checkup
	{
		public VisualStudioWindowsCheckup(string minimumVersion, string exactVersion = null)
		{
			MinimumVersion = NuGetVersion.Parse(minimumVersion);
			ExactVersion = exactVersion != null ? NuGetVersion.Parse(exactVersion) : null;
		}

		public override bool IsPlatformSupported(Platform platform)
			=> platform == Platform.Windows;

		public NuGetVersion MinimumVersion { get; private set; } = new NuGetVersion("16.9.0");
		public NuGetVersion ExactVersion { get; private set; }

		public override string Id => "visualstudio";

		public override string Title => $"Visual Studio {MinimumVersion.ThisOrExact(ExactVersion)}";

		public override async Task<DiagnosticResult> Examine(SharedState history)
		{
			var vsinfo = await GetWindowsInfo();

			var sentinelFiles = new List<string>();

			foreach (var vi in vsinfo)
			{
				if (vi.Version.IsCompatible(MinimumVersion, ExactVersion))
				{
					ReportStatus($"{vi.Version} - {vi.Path}", Status.Ok);

					var sentinel = Path.Combine(vi.Path, "MSBuild\\Current\\Bin\\SdkResolvers\\Microsoft.DotNet.MSBuildSdkResolver\\EnableWorkloadResolver.sentinel");
					sentinelFiles.Add(sentinel);
				}
				else
					ReportStatus($"{vi.Version}", null);
			}

			if (sentinelFiles.Any())
				history.ContributeState(this, "sentinel_files", sentinelFiles.ToArray());

			if (vsinfo.Any(vs => vs.Version.IsCompatible(MinimumVersion, ExactVersion)))
				return DiagnosticResult.Ok(this);

			return new DiagnosticResult(Status.Error, this);
		}

		Task<IEnumerable<VisualStudioInfo>> GetWindowsInfo()
		{
			var items = new List<VisualStudioInfo>();

			var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
				"Microsoft Visual Studio", "Installer", "vswhere.exe");


			if (!File.Exists(path))
				Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
				"Microsoft Visual Studio", "Installer", "vswhere.exe");

			if (!File.Exists(path))
				return default;

			var r = ShellProcessRunner.Run(path,
				"-all -requires Microsoft.Component.MSBuild -format json -prerelease");

			var str = r.GetOutput();

			var json = JsonDocument.Parse(str);

			foreach (var vsjson in json.RootElement.EnumerateArray())
			{
				if (!vsjson.TryGetProperty("catalog", out var vsCat) || !vsCat.TryGetProperty("productSemanticVersion", out var vsSemVer))
					continue;

				if (!NuGetVersion.TryParse(vsSemVer.GetString(), out var semVer))
					continue;

				if (!vsjson.TryGetProperty("installationPath", out var installPath))
					continue;

				items.Add(new VisualStudioInfo
				{
					Version = semVer,
					Path = installPath.GetString()
				});
			}

			return Task.FromResult<IEnumerable<VisualStudioInfo>>(items);
		}
	}

	public struct VisualStudioInfo
	{
		public string Path { get; set; }

		public NuGetVersion Version { get; set; }
	}
}
