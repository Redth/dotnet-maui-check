using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DotNetCheck
{
	public class ShellProcessRunnerOptions
	{
		public ShellProcessRunnerOptions(string exe, string args)
			: this(exe, args, CancellationToken.None)
		{
		}

		public ShellProcessRunnerOptions(string exe, string args, CancellationToken cancellationToken)
		{
			Executable = exe;
			Args = args;
			CancellationToken = cancellationToken;
		}
		 
		public string Executable { get; set; }
		public string Args { get; set; }

		public CancellationToken CancellationToken { get; set; } = CancellationToken.None;

		public Action<string> OutputCallback { get; set; }

		public bool RedirectOutput { get; set; } = true;

		public bool RedirectInput { get; set; } = false;

		public bool UseSystemShell { get; set; } = true;
	}

	public class ShellProcessRunner
	{
		public static ShellProcessResult Run(string executable, string args)
		{
			var p = new ShellProcessRunner(new ShellProcessRunnerOptions(executable, args));
			return p.WaitForExit();
		}

		readonly List<string> standardOutput;
		readonly List<string> standardError;
		readonly Process process;

		public readonly ShellProcessRunnerOptions Options;

		public ShellProcessRunner(ShellProcessRunnerOptions options)
		{
			Options = options;
			standardOutput = new List<string>();
			standardError = new List<string>();

			process = new Process();

			if (Options.UseSystemShell)
			{
				string tmpFile = null;

				if (!Util.IsWindows)
				{
					tmpFile = Path.GetTempFileName();
					File.WriteAllText(tmpFile, $"\"{Options.Executable}\" {Options.Args}");
				}

				// process.StartInfo.FileName = Util.IsWindows ? "cmd.exe" : (File.Exists("/bin/zsh") ? "/bin/zsh" : "/bin/bash");
				// process.StartInfo.Arguments = Util.IsWindows ? $"/c \"{executable} {args}\"" : $"-c \"{executable} {args}\"";
				process.StartInfo.FileName = Util.IsWindows ? Options.Executable : (File.Exists("/bin/zsh") ? "/bin/zsh" : "/bin/bash");
				process.StartInfo.Arguments = Util.IsWindows ? Options.Args : tmpFile;
			}
			else
			{
				process.StartInfo.FileName = Options.Executable;
				process.StartInfo.Arguments = Options.Args;
			}

			process.StartInfo.UseShellExecute = false;

			if (Options.RedirectOutput)
			{
				process.StartInfo.RedirectStandardOutput = true;
				process.StartInfo.RedirectStandardError = true;
			}

			// Process any env variables to be set that might have been set by other checkups
			// ie: JavaJdkCheckup sets MAUI_DOCTOR_JAVA_HOME
			foreach (var ev in Util.EnvironmentVariables)
				process.StartInfo.Environment[ev.Key] = ev.Value?.ToString();

			if (Options.RedirectInput)
				process.StartInfo.RedirectStandardInput = true;

			process.OutputDataReceived += (s, e) =>
			{
				if (e.Data != null)
				{
					standardOutput.Add(e.Data);
					Options?.OutputCallback?.Invoke(e.Data);
				}
			};
			process.ErrorDataReceived += (s, e) =>
			{
				if (e.Data != null)
				{
					standardError.Add(e.Data);
					Options?.OutputCallback?.Invoke(e.Data);
				}
			};
			process.Start();

			if (Options.RedirectOutput)
			{
				process.BeginOutputReadLine();
				process.BeginErrorReadLine();
			}

			if (Options.CancellationToken != System.Threading.CancellationToken.None)
			{
				Options.CancellationToken.Register(() =>
				{
					try { process.Kill(); }
					catch { }

					try { process?.Dispose(); }
					catch { }
				});
			}
		}

		public void Write(string txt)
			=> process.StandardInput.Write(txt);

		public int ExitCode
			=> process.HasExited ? process.ExitCode : -1;

		public bool HasExited
			=> process?.HasExited ?? false;

		public void Kill()
			=> process?.Kill();

		public ShellProcessResult WaitForExit()
		{
			try
			{
				process.WaitForExit();
			} catch { }

			if (standardError?.Any(l => l?.Contains("error: more than one device/emulator") ?? false) ?? false)
				throw new Exception("More than one Device/Emulator detected, you must specify which Serial to target.");

			return new ShellProcessResult(standardOutput, standardError, process.ExitCode);
		}

		public async Task<ShellProcessResult> WaitForExitAsync()
		{
			await process.WaitForExitAsync();

			return new ShellProcessResult(standardOutput, standardError, process.ExitCode);
		}

		public class ShellProcessResult
		{
			public readonly List<string> StandardOutput;
			public readonly List<string> StandardError;

			public string GetOutput()
				=> string.Join(Environment.NewLine, StandardOutput.Concat(StandardError));

			public readonly int ExitCode;

			public bool Success
				=> ExitCode == 0;

			internal ShellProcessResult(List<string> stdOut, List<string> stdErr, int exitCode)
			{
				StandardOutput = stdOut;
				StandardError = stdErr;
				ExitCode = exitCode;
			}
		}
	}
}
