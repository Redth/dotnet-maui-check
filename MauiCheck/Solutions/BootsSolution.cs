using DotNetCheck.Models;
using System;
using System.Threading.Tasks;

namespace DotNetCheck.Solutions
{
	public class BootsSolution : Solution
	{
		public BootsSolution(string url, string title)
		{
			Url = url;
			Title = title;
		}

		public string Url { get; set; }
		public string Title { get; set; }

		public override async Task Implement(System.Threading.CancellationToken cancellationToken)
		{
			await base.Implement(cancellationToken);

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
