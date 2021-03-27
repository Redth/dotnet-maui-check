using DotNetCheck.Models;
using System;
using System.Threading.Tasks;

namespace DotNetCheck.Solutions
{
	public class BootsSolution : Solution
	{
		public BootsSolution(Uri url, string title)
		{
			Url = url;
			Title = title;
		}

		public Uri Url { get; set; }
		public string Title { get; set; }

		public override async Task Implement(System.Threading.CancellationToken cancellationToken)
		{
			await base.Implement(cancellationToken);

			ReportStatus($"Installing {Title ?? Url.ToString()}...");

			var boots = new Boots.Core.Bootstrapper
			{
				Url = Url.ToString(),
				Logger = System.IO.TextWriter.Null
			};

			try
			{
				await boots.Install(cancellationToken);
				ReportStatus($"Installed {Title ?? Url.ToString()}.");
			}
			catch (Exception ex)
			{
				Util.Exception(ex);
				ReportStatus($":warning: Installation failed for {Title ?? Url.ToString()}.");
				throw;
			}
		}
		
	}
}
