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


		public static bool IsWindows
			=> Platform == Platform.Windows;

		public static bool IsMac
			=> Platform == Platform.OSX;		

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

		public static async Task<bool> WrapWithShellCopy(string destination, bool isFile, Func<string, Task<bool>> wrapping)
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
			else
			{
				// going straight to destination, try to make sure directory exists
				try
				{
					Directory.CreateDirectory(isFile ? new FileInfo(destination).Directory.FullName : destination);
				}
				catch (Exception ex) { Util.Exception(ex); }
			}

			var r = await wrapping(intermediate);

			if (r)
			{
				// Copy a file to a destination as su
				//		sudo mkdir -p destDir && sudo cp -pP intermediate destination

				// Copy a folder recursively to the destination as su
				//		sudo mkdir -p destDir && sudo cp -pPR intermediate destination
				var args = isFile
					? $"-c 'sudo mkdir -p \"{destDir}\" && sudo cp -pP \"{intermediate}\" \"{destination}\"'"
					: $"-c 'sudo mkdir -p \"{destDir}\" && sudo cp -pPR \"{intermediate}/\" \"{destination}\"'";

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
	}

	public enum Platform
	{
		Windows,
		OSX,
		Linux
	}
}
