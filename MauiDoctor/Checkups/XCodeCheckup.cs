using System;
using System.Threading.Tasks;
using MauiDoctor.Doctoring;
using NuGet.Versioning;

namespace MauiDoctor.Checkups
{
	public class XCodeCheckup : Checkup
	{
		public XCodeCheckup(string minimumVersion, string exactVersion = null)
			: this(NuGetVersion.Parse(minimumVersion), string.IsNullOrEmpty(exactVersion) ? null : NuGetVersion.Parse(exactVersion))
		{
		}

		public XCodeCheckup(SemanticVersion minimumVersion, SemanticVersion exactVersion = null)
		{
			ExactVersion = exactVersion;
			MinimumVersion = minimumVersion;
		}

		public override bool IsPlatformSupported(Platform platform)
			=> platform == Platform.OSX;

		public SemanticVersion MinimumVersion { get; private set; } = NuGetVersion.Parse("12.3");

		public SemanticVersion ExactVersion { get; private set; }

		public override string Id => "xcode";

		public override string Title => $"XCode {MinimumVersion.ThisOrExact(ExactVersion)}";

		public override async Task<Diagonosis> Examine()
		{
			var xcode = new XCode();

			var info = await xcode.GetInfo();

			if (NuGetVersion.TryParse(info.Version?.ToString(), out var semVer))
			{
				if (semVer.IsCompatible(MinimumVersion, ExactVersion))
					return Diagonosis.Ok(this);
			}

			return new Diagonosis(Status.Error, this, new Prescription($"Download XCode {MinimumVersion.ThisOrExact(ExactVersion)}"));
		}
	}
}
