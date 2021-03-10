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

				using (var s = new System.IO.StringWriter())
				{
					var boots = new Boots.Core.Bootstrapper
					{
						Url = url.url,
						Logger = s
					};

					await boots.Install(cancellationToken);
				}
			}
		}
	}
}
