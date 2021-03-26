using DotNetCheck;
using DotNetCheck.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNetCheck.Checkups
{
	public class FilePermissionsCheckup : Checkup
	{
		public override string Id => "filepermissions";

		public override string Title => "File Permissions";

		public override bool IsPlatformSupported(Platform platform)
			=> !Util.IsWindows;

		public override IEnumerable<CheckupDependency> DeclareDependencies(IEnumerable<string> checkupIds)
			=> checkupIds
				.Where(c => c.StartsWith("dotnetpacks-", StringComparison.OrdinalIgnoreCase))
				.Select(c => new CheckupDependency(c, false));

		public override bool ShouldExamine(SharedState history)
		{
			base.ShouldExamine(history);

			return Manifest?.Check?.FilePermissions?.Any() ?? false;
		}

		public override Task<DiagnosticResult> Examine(SharedState history)
		{
			var permissions = Manifest.Check.FilePermissions.Where(p => p.IsCompatible());

			var ok = true;

			var env = history.GetEnvironmentVariables();

			foreach (var p in permissions)
			{
				foreach (var pattern in p.Patterns)
				{
					var fixedPattern = pattern;

					foreach (var ev in env)
						fixedPattern = fixedPattern.Replace($"${ev.Key}", ev.Value);

					var proc = new ShellProcessRunner(new ShellProcessRunnerOptions("/bin/chmod", $"-r +x {fixedPattern}")
					{
						UseSystemShell = false,
						EnvironmentVariables = env,
					});

					if (proc.WaitForExit().ExitCode != 0)
						ok = false;
				}
			}

			return Task.FromResult(new DiagnosticResult(Status.Ok, this));
		}
	}
}
