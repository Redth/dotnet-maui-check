using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MauiDoctor.Doctoring;
using NuGet.Versioning;

namespace MauiDoctor.Checkups
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
				{
					ReportStatus($"{vi.Version} - {vi.Path}", Status.Ok);

					var workloadResolverSentinel = Path.Combine(vi.Path, "MSBuild\\Current\\Bin\\SdkResolvers\\Microsoft.DotNet.MSBuildSdkResolver\\EnableWorkloadResolver.sentinel");

					if (Directory.Exists(workloadResolverSentinel) && !File.Exists(workloadResolverSentinel))
					{
						try
						{
							File.Create(workloadResolverSentinel);
							ReportStatus("Created EnableWorkloadResolver.sentinel for IDE support", Status.Ok);
						}
						catch { }
					}
				}
				else
					ReportStatus($"{vi.Version}", null);
			}

			if (vsinfo.Any(vs => vs.Version.IsCompatible(MinimumVersion, ExactVersion)))
				return Diagonosis.Ok(this);

			return new Diagonosis(Status.Error, this);
		}
	}
}
