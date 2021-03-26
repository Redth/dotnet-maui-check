using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace DotNetCheck
{
	public class Util
	{
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
	}

	public enum Platform
	{
		Windows,
		OSX,
		Linux
	}


}
