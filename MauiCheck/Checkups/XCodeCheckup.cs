using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DotNetCheck.Models;
using NuGet.Versioning;

namespace DotNetCheck.Checkups
{
	public class XCodeCheckup : Checkup
	{
		public override bool IsPlatformSupported(Platform platform)
			=> platform == Platform.OSX;

		public NuGetVersion MinimumVersion
			=> Extensions.ParseVersion(Manifest?.Check?.XCode?.MinimumVersion, new NuGetVersion("12.3"));

		public NuGetVersion ExactVersion
			=> Extensions.ParseVersion(Manifest?.Check?.XCode?.ExactVersion);

		public override string Id => "xcode";

		public override string Title => $"XCode {MinimumVersion.ThisOrExact(ExactVersion)}";

		public override bool ShouldExamine(SharedState history)
			=> Manifest?.Check?.XCode != null;

		public override async Task<DiagnosticResult> Examine(SharedState history)
		{
			var info = await GetInfo();

			if (NuGetVersion.TryParse(info.Version?.ToString(), out var semVer))
			{
				if (semVer.IsCompatible(MinimumVersion, ExactVersion))
				{
					ReportStatus($"XCode.app ({info.Version} {info.Build})", Status.Ok);
					return DiagnosticResult.Ok(this);
				}
			}

			ReportStatus($"XCode.app ({info.Version}) not installed.", Status.Error);

			return new DiagnosticResult(Status.Error, this, new Suggestion($"Download XCode {MinimumVersion.ThisOrExact(ExactVersion)}"));
		}

		Task<XCodeInfo> GetInfo()
		{
			//Xcode 12.4
			//Build version 12D4e
			var r = ShellProcessRunner.Run("xcodebuild", "-version");

			var info = new XCodeInfo();

			foreach (var line in r.StandardOutput)
			{
				if (line.StartsWith("Xcode"))
				{
					var vstr = line.Substring(5).Trim();
					if (Version.TryParse(vstr, out var v))
						info.Version = v;
				}
				else if (line.StartsWith("Build version"))
				{
					info.Build = line.Substring(13)?.Trim();
				}
			}

			return Task.FromResult(info);
		}
	}

	public struct XCodeInfo
	{
		public Version Version;
		public string Build;
	}
}
