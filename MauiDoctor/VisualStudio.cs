using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Claunia.PropertyList;
using NuGet.Versioning;

namespace MauiDoctor
{
	public class VisualStudio
	{
		public VisualStudio()
		{
		}


		public Task<IEnumerable<VisualStudioInfo>> GetInfo()
		{
			if (Util.IsWindows)
				return GetWindowsInfo();
			if (Util.IsMac)
				return GetMacInfo();

			return default;
		}

		public Task<IEnumerable<VisualStudioInfo>> GetMacInfo()
		{
			var items = new List<VisualStudioInfo>();

			var likelyPaths = new List<string> {
				"/Applications/Visual Studio.app/"
			};


			foreach (var likelyPath in likelyPaths)
			{
				var path = Path.Combine(likelyPath, "Contents", "Info.plist");

				if (File.Exists(path))
				{
					var plist = (NSDictionary)Claunia.PropertyList.PropertyListParser.Parse(path);

					var bvs = plist["CFBundleVersion"].ToString();

					if (!string.IsNullOrEmpty(bvs) && NuGetVersion.TryParse(bvs, out var ver))
						items.Add(new VisualStudioInfo
						{
							Path = likelyPath,
							Version = ver
						});
				}
			}

			return Task.FromResult<IEnumerable<VisualStudioInfo>>(items);
		}

		public Task<IEnumerable<VisualStudioInfo>> GetWindowsInfo()
		{
			var items = new List<VisualStudioInfo>();

			var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
				"Microsoft Visual Studio", "Installer", "vswhere.exe");


			if (!File.Exists(path))
				Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
				"Microsoft Visual Studio", "Installer", "vswhere.exe");

			if (!File.Exists(path))
				return default;

			var r = ShellProcessRunner.Run(path,
				"-all -requires Microsoft.Component.MSBuild -format json -prerelease");

			var str = r.GetOutput();

			var json = JsonDocument.Parse(str);

			foreach (var vsjson in json.RootElement.EnumerateArray())
			{
				if (!vsjson.TryGetProperty("catalog", out var vsCat) || !vsCat.TryGetProperty("productSemanticVersion", out var vsSemVer))
					continue;

				if (!NuGetVersion.TryParse(vsSemVer.GetString(), out var semVer))
					continue;

				if (!vsjson.TryGetProperty("installationPath", out var installPath))
					continue;

				items.Add(new VisualStudioInfo
				{
					Version = semVer,
					Path = installPath.GetString()
				});
			}

			return Task.FromResult<IEnumerable<VisualStudioInfo>>(items);
		}
	}

	public struct VisualStudioInfo
	{
		public string Path { get; set; }

		public NuGetVersion Version { get; set; }
	}
}
