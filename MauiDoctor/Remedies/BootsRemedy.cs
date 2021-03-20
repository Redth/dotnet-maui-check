using System;
using System.Threading.Tasks;

namespace MauiDoctor.Doctoring
{
	public class BootsRemedy : Remedy
	{
		public BootsRemedy(string url, string title)
		{
			Url = url;
			Title = title;
		}

		public string Url { get; set; }
		public string Title { get; set; }

		public override async Task Cure(System.Threading.CancellationToken cancellationToken)
		{
			await base.Cure(cancellationToken);

			ReportStatus($"Installing {Title ?? Url}...");

			var boots = new Boots.Core.Bootstrapper
			{
				Url = Url,
				Logger = System.IO.TextWriter.Null
			};

			try
			{
				await boots.Install(cancellationToken);
				ReportStatus($"Installed {Title ?? Url}.");
			}
			catch
			{
				ReportStatus($":warning: Installation failed for {Title ?? Url}.");
				throw;
			}
		}
		
	}
}
