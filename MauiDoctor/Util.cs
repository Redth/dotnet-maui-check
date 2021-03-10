using System;
using System.Runtime.InteropServices;

namespace MauiDoctor
{
	public class Util
	{
		public Util()
		{
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

	}

	public enum Platform
	{
		Windows,
		OSX,
		Linux
	}


}
