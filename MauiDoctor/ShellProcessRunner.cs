using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MauiDoctor
{
	public class ShellProcessRunner
	{

		public static ShellProcessResult Run(string executable, string args)
		{
			var p = new ShellProcessRunner(executable, args, System.Threading.CancellationToken.None);
			return p.WaitForExit();
		}

		readonly List<string> standardOutput;
		readonly List<string> standardError;
		readonly Process process;

		public Action<string> OutputHandler { get; private set; }

		public ShellProcessRunner(string executable, string args, System.Threading.CancellationToken cancellationToken, Action<string> outputHandler = null)
		{
			OutputHandler = outputHandler;

			standardOutput = new List<string>();
			standardError = new List<string>();

			process = new Process();
			// process.StartInfo.FileName = Util.IsWindows ? "cmd.exe" : (File.Exists("/bin/zsh") ? "/bin/zsh" : "/bin/bash");
			// process.StartInfo.Arguments = Util.IsWindows ? $"/c \"{executable} {args}\"" : $"-c \"{executable} {args}\"";
			process.StartInfo.FileName = Util.IsWindows ? executable : (File.Exists("/bin/zsh") ? "/bin/zsh" : "/bin/bash");
			process.StartInfo.Arguments = Util.IsWindows ? args : $"-c \"{executable} {args}\"";
			process.StartInfo.UseShellExecute = false;
			process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.RedirectStandardError = true;

			process.OutputDataReceived += (s, e) =>
			{
				if (e.Data != null)
				{
					standardOutput.Add(e.Data);
					OutputHandler?.Invoke(e.Data);
				}
			};
			process.ErrorDataReceived += (s, e) =>
			{
				if (e.Data != null)
				{
					standardError.Add(e.Data);
					OutputHandler?.Invoke(e.Data);
				}
			};
			process.Start();
			process.BeginOutputReadLine();
			process.BeginErrorReadLine();

			if (cancellationToken != System.Threading.CancellationToken.None)
			{
				cancellationToken.Register(() =>
				{
					try { process.Kill(); }
					catch { }
				});
			}
		}

		public int ExitCode
			=> process.HasExited ? process.ExitCode : -1;

		public bool HasExited
			=> process?.HasExited ?? false;

		public void Kill()
			=> process?.Kill();

		public ShellProcessResult WaitForExit()
		{
			process.WaitForExit();

			return new ShellProcessResult(standardOutput, standardError, process.ExitCode);
		}

		public Task<ShellProcessResult> WaitForExitAsync()
		{
			var tcs = new TaskCompletionSource<ShellProcessResult>();

			Task.Run(() =>
			{
				var r = WaitForExit();
				tcs.TrySetResult(r);
			});

			return tcs.Task;
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
