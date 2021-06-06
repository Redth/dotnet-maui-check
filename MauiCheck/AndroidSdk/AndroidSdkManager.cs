﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace DotNetCheck.AndroidSdk
{
	public class AndroidSdkManager
	{
		static string[] KnownLikelyPaths =>
			RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ?
				new string[] {
					Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Android", "android-sdk"),
					Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Android", "android-sdk"),
					Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Android", "sdk"),
					Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Android", "sdk"),
					Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "AppData", "Local", "Android", "sdk"),
				} :
				new string []
				{
					Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Developer", "android-sdk-macosx"),
					Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Developer", "Xamarin", "android-sdk-macosx"),
					Path.Combine("Developer", "Android", "android-sdk-macosx"),
				};

		public static IEnumerable<DirectoryInfo> FindHome()
			=> FindHome((string)null, null);

		public static IEnumerable<DirectoryInfo> FindHome(DirectoryInfo mostLikelyHome = null)
			=> FindHome(mostLikelyHome?.FullName, null);

		public static IEnumerable<DirectoryInfo> FindHome(DirectoryInfo mostLikelyHome = null, params string[] additionalPossibleDirectories)
			=> FindHome(mostLikelyHome?.FullName, additionalPossibleDirectories);

		public static IEnumerable<DirectoryInfo> FindHome(string mostLikelyHome = null, params string[] additionalPossibleDirectories)
		{
			var candidates = new List<string>();
			
			candidates.Add(mostLikelyHome);
			candidates.Add(Environment.GetEnvironmentVariable("ANDROID_SDK_ROOT"));
			candidates.Add(Environment.GetEnvironmentVariable("ANDROID_HOME"));
			
			if (additionalPossibleDirectories != null)
				candidates.AddRange(additionalPossibleDirectories);
			candidates.AddRange(KnownLikelyPaths);

			foreach (var c in candidates.Distinct())
			{
				if (!string.IsNullOrWhiteSpace(c) && Directory.Exists(c))
					yield return new DirectoryInfo(c);
			}
		}

		public AndroidSdkManager(string home = null)
		{
			DirectoryInfo homeDir = null;

			if (!string.IsNullOrEmpty(home) && Directory.Exists(home))
				homeDir = new DirectoryInfo(home);

			Home = homeDir ?? FindHome()?.FirstOrDefault();

			AvdManager = new AvdManager(Home);
		}

		public readonly DirectoryInfo Home;

		public readonly AvdManager AvdManager;
	}
}
