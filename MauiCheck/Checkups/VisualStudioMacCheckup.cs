using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Claunia.PropertyList;
using DotNetCheck.Models;
using NuGet.Versioning;

namespace DotNetCheck.Checkups
{
	public class VisualStudioMacCheckup : Checkup
	{
		public override bool IsPlatformSupported(Platform platform)
			=> platform == Platform.OSX;

		public NuGetVersion MinimumVersion
			=> Extensions.ParseVersion(Manifest?.Check?.VSMac?.MinimumVersion, new NuGetVersion("8.9.0"));

		public NuGetVersion ExactVersion
			=> Extensions.ParseVersion(Manifest?.Check?.VSMac?.ExactVersion);

		public bool Optional
			=> Manifest?.Check?.VSMac?.Optional ?? false;

		public override string Id => "vsmac";

		public override string Title => $"Visual Studio {MinimumVersion.ThisOrExact(ExactVersion)}";

		public override bool ShouldExamine(SharedState history)
			=> Manifest?.Check?.VSMac != null;

		public override async Task<DiagnosticResult> Examine(SharedState history)
		{
			var vsinfo = await GetMacInfo();

			var ok = false;

			foreach (var vs in vsinfo)
			{
				if (vs.Version.IsCompatible(MinimumVersion, ExactVersion))
				{
					ok = true;
					ReportStatus($"Visual Studio for Mac ({vs.Version})", Status.Ok);
				}
				else
				{
					ReportStatus($"Visual Studio for Mac ({vs.Version})", null);
				}
			}


			// Check VSCode sentinel files, ie:
			// ~/.vscode/extensions/ms-dotnettools.csharp-1.23.9/.omnisharp/1.37.8-beta.15/omnisharp/.msbuild/Current/Bin

			var vscodeExtPath = Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
				".vscode",
				"extensions");

			var sentinelFiles = new List<string>();
			var vscodeExtDir = new DirectoryInfo(vscodeExtPath);

			if (vscodeExtDir.Exists)
			{
				var sdkResolverDirs = Directory.EnumerateDirectories(vscodeExtPath, "*Microsoft.DotNet.MSBuildSdkResolver", SearchOption.AllDirectories);

				if (sdkResolverDirs?.Any() ?? false)
				{
					foreach (var r in sdkResolverDirs)
					{
						if (!Directory.Exists(r))
							continue;

						var sentinelFile = Path.Combine(r, "EnableWorkloadResolver.sentinel");

						sentinelFiles.Add(sentinelFile);
					}
				}
			}

			if (sentinelFiles.Any())
				history.ContributeState(this, "sentinel_files", sentinelFiles.ToArray());

			if (ok || Optional)
				return DiagnosticResult.Ok(this);

			return new DiagnosticResult(Status.Error, this);
		}

		Task<IEnumerable<VisualStudioInfo>> GetMacInfo()
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
	}
}
