using System;
using System.Linq;
using System.Threading.Tasks;
using MauiDoctor.Doctoring;
using NuGet.Versioning;

namespace MauiDoctor.Checkups
{
	public class VisualStudioMacCheckup : Checkup
	{
		public VisualStudioMacCheckup(string minimumVersion, string exactVersion = null)
		{
			MinimumVersion = NuGetVersion.Parse(minimumVersion);
			ExactVersion = exactVersion != null ? NuGetVersion.Parse(exactVersion) : null;
		}

		public override bool IsPlatformSupported(Platform platform)
			=> platform == Platform.OSX;

		public NuGetVersion MinimumVersion { get; private set; } = new NuGetVersion("8.9.0");
		public NuGetVersion ExactVersion { get; private set; }

		public override string Id => "visuastudio";

		public override string Title => $"Visual Studio {MinimumVersion.ThisOrExact(ExactVersion)}";

		public override async Task<Diagonosis> Examine()
		{
			var vs = new VisualStudio();

			var vsinfo = await vs.GetMacInfo();

			if (vsinfo.Any(vs => vs.Version.IsCompatible(MinimumVersion, ExactVersion)))
				return Diagonosis.Ok(this);

			return new Diagonosis(Status.Error, this);
		}
	}
}
