using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace MauiDoctor
{
	public class Util
	{
		public Util()
		{
		}

		public const string DoctorEnvironmentVariableNamePrefix = "MAUI_DOCTOR_";

		public static void SetDoctorEnvironmentVariable(string name, string value)
		{
			Environment.SetEnvironmentVariable(name, value);
		}
		public static string GetDoctorEnvironmentVariable(string name)
		{
			foreach (DictionaryEntry ev in Environment.GetEnvironmentVariables())
			{
				if (ev.Key.ToString().StartsWith(DoctorEnvironmentVariableNamePrefix, StringComparison.OrdinalIgnoreCase))
				{
					var evName = ev.Key.ToString().Substring(DoctorEnvironmentVariableNamePrefix.Length);

					if (evName.Equals(name, StringComparison.OrdinalIgnoreCase))
						return ev.Value?.ToString();
				}
			}

			return null;
		}

		public static IEnumerable<KeyValuePair<string, string>> GetDoctorEnvironmentVariables()
		{
			foreach (DictionaryEntry ev in Environment.GetEnvironmentVariables())
			{
				var key = ev.Key.ToString();

				if (key?.StartsWith(DoctorEnvironmentVariableNamePrefix, StringComparison.OrdinalIgnoreCase) ?? false)
				{
					var v = ev.Value?.ToString();
					if (!string.IsNullOrEmpty(v))
						yield return new KeyValuePair<string, string>(key, v);
				}
			}
		}

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
