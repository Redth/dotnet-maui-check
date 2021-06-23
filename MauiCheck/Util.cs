using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Spectre.Console;

namespace DotNetCheck
{
	public class Util
	{
		public static void LogAlways(string message)
		{
			Console.WriteLine(message);
		}

		public static void Log(string message)
		{
			if (Verbose)
				Console.WriteLine(message);
		}

		public static void Exception(Exception ex)
		{
			if (Verbose)
				AnsiConsole.WriteException(ex);
		}

		public static bool Verbose { get; set; }
		public static bool CI { get; set; }

		public static Dictionary<string, string> EnvironmentVariables { get; } = new Dictionary<string, string>();

		public static Platform Platform
		{
			get
			{

				if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
					return Platform.Windows;

				if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
					return Platform.OSX;

				if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD))
					return Platform.Linux;

				return Platform.Windows;
			}
		}

		public static bool Is64
		{
			get
			{
				if (Platform == Platform.Windows && RuntimeInformation.OSArchitecture == Architecture.X86)
					return false;
				return true;
			}
		}

		public static bool IsArm64
			=> RuntimeInformation.OSArchitecture == Architecture.Arm64;

		public static bool IsWindows
			=> Platform == Platform.Windows;

		public static bool IsMac
			=> Platform == Platform.OSX;

		public const string ArchWin = "win";
		public const string ArchWin64 = "win64";
		public const string ArchWinArm64 = "winArm64";
		public const string ArchOsx = "osx";
		public const string ArchOsxArm64 = "osxArm64";

		public static bool IsArchCompatible(string arch)
        {
			if (string.IsNullOrEmpty(arch))
				return true;

			arch = arch.ToLowerInvariant().Trim();

			if (arch == ArchWin)
				return IsWindows && !Is64 && !IsArm64;
			else if(arch == ArchWin64)
				return IsWindows && Is64 && !IsArm64;
			else if (arch == ArchWinArm64)
				return IsWindows && IsArm64;
			else if (arch == ArchOsx)
				return IsMac && !IsArm64;
			else if (arch == ArchOsxArm64)
				return IsMac && IsArm64;

			return false;
		}

		[DllImport("libc")]
#pragma warning disable IDE1006 // Naming Styles
		static extern uint getuid();
#pragma warning restore IDE1006 // Naming Styles


		public static bool IsAdmin()
		{
			try
			{
				if (IsWindows)
				{
					using (var identity = System.Security.Principal.WindowsIdentity.GetCurrent())
					{
						var principal = new System.Security.Principal.WindowsPrincipal(identity);
						if (!principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator))
						{
							return false;
						}
					}
				}
				else if (getuid() != 0)
				{
					return false;
				}
			}
			catch
			{
				return false;
			}

			return true;
		}

		public static bool Delete(string path, bool isFile)
		{
			if (!Util.IsWindows)
			{
				// Delete the destination as su
				//		sudo rm -rf destination
				var args = $"-c 'sudo rm -rf \"{path}\"'";

				if (Verbose)
					Console.WriteLine($"{ShellProcessRunner.MacOSShell} {args}");

				var p = new ShellProcessRunner(new ShellProcessRunnerOptions(ShellProcessRunner.MacOSShell, args)
				{
					RedirectOutput = Verbose
				});

				p.WaitForExit();

				return true;
			}
			else
			{
				try
				{
					if (isFile)
						File.Delete(path);
					else
						Directory.Delete(path, true);

					return true;
				}
				catch (Exception ex)
				{
					Util.Exception(ex);
				}
			}

			return false;
		}

		public static Task<ShellProcessRunner.ShellProcessResult> WrapShellCommandWithSudo(string cmd, string[] args)
		{
			var actualCmd = cmd;
			var actualArgs = string.Join(" ", args);

			if (!Util.IsWindows)
			{
				actualCmd = ShellProcessRunner.MacOSShell;
				actualArgs = $"-c 'sudo {cmd} {actualArgs}'"; 
			}

			var cli = new ShellProcessRunner(new ShellProcessRunnerOptions(actualCmd, actualArgs));
			return Task.FromResult(cli.WaitForExit());
		}

		public static async Task<bool> WrapCopyWithShellSudo(string destination, bool isFile, Func<string, Task<bool>> wrapping)
		{
			var intermediate = destination;

			var destDir = intermediate;
			if (isFile)
				destDir = new FileInfo(destDir).Directory.FullName;

			if (!Util.IsWindows)
			{
				if (isFile)
					intermediate = Path.Combine(Path.GetTempFileName());
				else
					intermediate = Path.Combine(Path.GetTempPath(), System.Guid.NewGuid().ToString());
			}

			// If windows, we'll delete the directory or file at destination here
			if (Util.IsWindows)
			{
				var dir = isFile ? new FileInfo(destination).Directory.FullName : new DirectoryInfo(destination).FullName;

				if (!Directory.Exists(dir))
					Directory.CreateDirectory(dir);
			}

			var r = await wrapping(intermediate);

			if (r && !Util.IsWindows)
			{
				// Copy a file to a destination as su
				//		sudo mkdir -p destDir && sudo cp -pP intermediate destination

				// Copy a folder recursively to the destination as su
				//		sudo mkdir -p destDir && sudo cp -pPR intermediate/ destination
				var args = isFile
					? $"-c 'sudo mkdir -p \"{destDir}\" && sudo cp -pP \"{intermediate}\" \"{destination}\"'"
					: $"-c 'sudo mkdir -p \"{destDir}\" && sudo cp -pPR \"{intermediate}/\" \"{destination}\"'"; // note the / at the end of the dir

				if (Verbose)
					Console.WriteLine($"{ShellProcessRunner.MacOSShell} {args}");

				var p = new ShellProcessRunner(new ShellProcessRunnerOptions(ShellProcessRunner.MacOSShell, args)
				{
					RedirectOutput = Verbose
				});

				p.WaitForExit();
			}

			return r;
		}

		public static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
		{
			// Get the subdirectories for the specified directory.
			var dir = new DirectoryInfo(sourceDirName);

			if (!dir.Exists)
				return;

			var dirs = dir.GetDirectories();

			// If the destination directory doesn't exist, create it.
			Directory.CreateDirectory(destDirName);

			// Get the files in the directory and copy them to the new location.
			var files = dir.GetFiles();
			foreach (var file in files)
			{
				var tempPath = Path.Combine(destDirName, file.Name);
				file.CopyTo(tempPath, false);
			}

			// If copying subdirectories, copy them and their contents to new location.
			if (copySubDirs)
			{
				foreach (var subdir in dirs)
				{
					var tempPath = Path.Combine(destDirName, subdir.Name);
					DirectoryCopy(subdir.FullName, tempPath, copySubDirs);
				}
			}
		}
	}

	public enum Platform
	{
		Windows,
		OSX,
		Linux
	}
}
