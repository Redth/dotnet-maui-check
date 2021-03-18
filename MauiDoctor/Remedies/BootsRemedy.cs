using System;
using System.Threading.Tasks;

namespace MauiDoctor.Doctoring
{
	public class BootsRemedy : Remedy
	{
		public BootsRemedy(params (string url, string title)[] urls)
		{
			Urls = urls;
		}

		public (string url, string title)[] Urls { get; private set; }

		public override async Task Cure(System.Threading.CancellationToken cancellationToken)
		{
			await base.Cure(cancellationToken);

			int i = 0;
			foreach (var url in Urls)
			{
				i++;

				if (string.IsNullOrEmpty(url.url))
					continue;

				ReportStatus($"Installing {url.title ?? url.url}", i / Urls.Length);

				var boots = new Boots.Core.Bootstrapper
				{
					Url = url.url,
					Logger = System.IO.TextWriter.Null
				};

				try
				{
					await boots.Install(cancellationToken);
				}
				catch
				{
					if (string.IsNullOrEmpty(url.title))
						ReportStatus($":warning: Installation failed for an item:", i / Urls.Length);
					else
						ReportStatus($":warning: Installation failed for {url.title}", i / Urls.Length);
					ReportStatus($"  {url.url}", i / Urls.Length);

					throw;
				}
			}
		}
	}
}
