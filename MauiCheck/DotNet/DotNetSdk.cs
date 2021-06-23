using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using DotNetCheck.Models;
using Microsoft.NET.Sdk.WorkloadManifestReader;
using NuGet.Common;
using NuGet.Packaging.Core;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace DotNetCheck.DotNet
{
	public class DotNetSdk
	{
		public readonly string[] KnownDotnetLocations;

		public readonly FileInfo DotNetExeLocation;
		public readonly DirectoryInfo DotNetSdkLocation;

		public static string DotNetExeName
			=> Util.IsWindows ? "dotnet.exe" : "dotnet";

		public DotNetSdk(SharedState sharedState)
		{
			KnownDotnetLocations = Util.Platform switch
			{
				Platform.Windows => new string[]
				{
					Path.Combine(
						Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
						"dotnet",
						DotNetExeName),
					Path.Combine(
						Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
						"dotnet",
						DotNetExeName),
				},
				Platform.OSX => new string[]
				{
					"/usr/local/share/dotnet/dotnet",
				},
				Platform.Linux => new string[]
				{
					// /home/user/share/dotnet/dotnet
					Path.Combine(
						Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
						"share",
						"dotnet",
						DotNetExeName)
				},
				_ => new string[] { }
			};

			string sdkRoot = null;

			if (sharedState != null && sharedState.TryGetEnvironmentVariable("DOTNET_ROOT", out var envSdkRoot))
			{
				if (Directory.Exists(envSdkRoot))
					sdkRoot = envSdkRoot;
			}

			if (string.IsNullOrEmpty(sdkRoot) || !Directory.Exists(sdkRoot))
			{
				sdkRoot = Microsoft.DotNet.NativeWrapper.EnvironmentProvider.GetDotnetExeDirectory();
			}

			if (string.IsNullOrEmpty(sdkRoot) || !Directory.Exists(sdkRoot))
			{
				var l = FindDotNetLocations();
				if (l != default)
				{
					sdkRoot = l.sdkDir.FullName;
				}
			}

			sharedState.SetEnvironmentVariable("DOTNET_ROOT", sdkRoot);

			// First try and use the actual resolver logic
			DotNetSdkLocation = new DirectoryInfo(sdkRoot);
			DotNetExeLocation = new FileInfo(Path.Combine(DotNetSdkLocation.FullName, DotNetExeName));
		}

		public bool Exists
			=> DotNetExeLocation != null && DotNetExeLocation.Exists;

		(DirectoryInfo sdkDir, FileInfo dotnet) FindDotNetLocations()
		{
			foreach (var dotnetLoc in KnownDotnetLocations)
			{
				if (File.Exists(dotnetLoc))
				{
					var dotnet = new FileInfo(dotnetLoc);

					return (dotnet.Directory, dotnet);
				}
			}

			return default;
		}

		public Task<IEnumerable<DotNetSdkInfo>> GetSdks()
		{
			var r = ShellProcessRunner.Run(DotNetExeLocation.FullName, "--list-sdks");

			var sdks = new List<DotNetSdkInfo>();

			foreach (var line in r.StandardOutput)
			{
				try
				{
					if (line.Contains('[') && line.Contains(']'))
					{
						var versionStr = line.Substring(0, line.IndexOf('[')).Trim();

						var locStr = line.Substring(line.IndexOf('[')).Trim('[', ']');

						if (Directory.Exists(locStr))
						{
							var loc = Path.Combine(locStr, versionStr);
							if (Directory.Exists(loc))
							{
								// If only 1 file it's probably the
								// EnableWorkloadResolver.sentinel file that was 
								// never uninstalled with the rest of the sdk
								if (Directory.GetFiles(loc)?.Length > 1)
								{
									if (NuGetVersion.TryParse(versionStr, out var version))
										sdks.Add(new DotNetSdkInfo(version, new DirectoryInfo(loc)));
								}
							}
							else
								Util.Log($"Directory does not exist: {loc}");
						}
						else
							Util.Log($"Directory does not exist: {locStr}");
					}
				}
				catch
				{
					// Bad line, ignore
				}
			}

			return Task.FromResult<IEnumerable<DotNetSdkInfo>>(sdks);
		}
	}

	public class DotNetSdkInfo
	{
		public DotNetSdkInfo(string version, string directory)
			: this(NuGetVersion.Parse(version), new DirectoryInfo(directory))
		{ }

		public DotNetSdkInfo(NuGetVersion version, DirectoryInfo directory)
		{
			Version = version;
			Directory = directory;
		}

		public NuGetVersion Version { get; set; }

		public DirectoryInfo Directory{ get; set; }
	}
}
