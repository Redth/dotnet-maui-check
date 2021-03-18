using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.NET.Sdk.WorkloadManifestReader;
using NuGet.Common;
using NuGet.Packaging.Core;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace MauiDoctor
{
	public class DotNet
	{
		public readonly string[] KnownDotnetLocations;

		public readonly FileInfo DotNetExeLocation;
		public readonly DirectoryInfo DotNetSdkLocation;

		public DotNet()
		{
			KnownDotnetLocations = Util.Platform switch
			{
				Platform.Windows => new string[]
				{
					Path.Combine(
						Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
						"dotnet",
						"dotnet.exe"),
					Path.Combine(
						Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
						"dotnet",
						"dotnet.exe"),
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
						"dotnet")
				},
				_ => new string[] { }
			};

			// First try and use the actual resolver logic
			var r = new Microsoft.DotNet.DotNetSdkResolver.NETCoreSdkResolver();
			DotNetSdkLocation = new DirectoryInfo(r.GetDotnetExeDirectory());

			if (DotNetSdkLocation.Exists)
			{
				DotNetExeLocation = new FileInfo(Path.Combine(DotNetSdkLocation.FullName, Util.IsWindows ? "dotnet.exe" : "dotnet"));
			}
			else
			{
				var l = FindDotNetLocations();

				if (l != default)
				{
					DotNetExeLocation = l.dotnet;
					DotNetSdkLocation = l.sdkDir;
				}
			}
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
								if (NuGetVersion.TryParse(versionStr, out var version))
									sdks.Add(new DotNetSdkInfo(version, new DirectoryInfo(loc)));
							}
						}
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
