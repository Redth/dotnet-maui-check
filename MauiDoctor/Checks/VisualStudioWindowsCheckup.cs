using System;
using System.Linq;
using System.Threading.Tasks;
using MauiDoctor.Doctoring;
using NuGet.Versioning;

namespace MauiDoctor.Checks
{
	public class VisualStudioWindowsCheckup : Checkup
	{
		public VisualStudioWindowsCheckup(string minimumVersion, string exactVersion = null)
		{
			MinimumVersion = NuGetVersion.Parse(minimumVersion);
			ExactVersion = exactVersion != null ? NuGetVersion.Parse(exactVersion) : null;
		}

		public override bool IsPlatformSupported(Platform platform)
			=> platform == Platform.Windows;

		public NuGetVersion MinimumVersion { get; private set; } = new NuGetVersion("16.9.0");
		public NuGetVersion ExactVersion { get; private set; }

		public override string Id => "visuastudio";

		public override string Title => $"Visual Studio {MinimumVersion.ThisOrExact(ExactVersion)}";

		public override async Task<Diagonosis> Examine()
		{
			var vs = new VisualStudio();

			var vsinfo = await vs.GetWindowsInfo();

			foreach (var vi in vsinfo)
			{
				if (vi.Version.IsCompatible(MinimumVersion, ExactVersion))
					ReportStatus("  :check_mark: [darkgreen]" + vi.Version + " - " + vi.Path + "[/]");
				else
					ReportStatus("  - [grey]" + vi.Version + "[/]");
			}

			if (vsinfo.Any(vs => vs.Version.IsCompatible(MinimumVersion, ExactVersion)))
				return Diagonosis.Ok(this);

			return new Diagonosis(Status.Error, this);
		}
	}
}
