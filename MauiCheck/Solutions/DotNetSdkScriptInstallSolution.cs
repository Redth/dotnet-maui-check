using DotNetCheck;
using DotNetCheck.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DotNetCheck.Solutions
{
	public class DotNetSdkScriptInstallSolution : Solution
	{
		const string installScriptBash = "https://dot.net/v1/dotnet-install.sh";
		const string installScriptPwsh = "https://dot.net/v1/dotnet-install.ps1";

		public DotNetSdkScriptInstallSolution(string version)
		{
			Version = version;
		}

		public readonly string Version;
		
		public override async Task Implement(SharedState sharedState, CancellationToken cancellationToken)
		{
			await base.Implement(sharedState, cancellationToken);

			string sdkRoot = default;

			if (sharedState != null && sharedState.TryGetEnvironmentVariable("DOTNET_ROOT", out var envSdkRoot))
			{
				if (Directory.Exists(envSdkRoot))
					sdkRoot = envSdkRoot;
			}

			if (string.IsNullOrEmpty(sdkRoot))
				sdkRoot = Util.IsWindows
					? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "dotnet")
					: Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".dotnet");

			var scriptUrl = Util.IsWindows ? installScriptPwsh : installScriptBash;
			var scriptPath = Path.Combine(Path.GetTempPath(), Util.IsWindows ? "dotnet-install.ps1" : "dotnet-install.sh");

			var http = new HttpClient();
			var data = await http.GetStringAsync(scriptUrl);
			File.WriteAllText(scriptPath, data);

			// Launch the process
			var p = new ShellProcessRunner(new ShellProcessRunnerOptions(
				Util.IsWindows ? "powershell" : ShellProcessRunner.MacOSShell,
				$"{scriptPath} --install-dir {sdkRoot} --version {Version}"));

			p.WaitForExit();
		}
	}
}
