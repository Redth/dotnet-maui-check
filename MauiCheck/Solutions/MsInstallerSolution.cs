using DotNetCheck.Models;
using Polly;
using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace DotNetCheck.Solutions
{
	public class MsInstallerSolution : Solution
	{
		public MsInstallerSolution(Uri url, string title)
		{
			Url = url;
			Title = title;
		}

		public Uri Url { get; set; }
		public string Title { get; set; }

		public override async Task Implement(SharedState sharedState, CancellationToken cancelToken)
		{
			await base.Implement(sharedState, cancelToken);

			var filename = Path.GetFileName(Url.AbsolutePath);
			string tmpExe = null;

			var http = new HttpClient();
			http.Timeout = TimeSpan.FromMinutes(45);

			await Policy
				.Handle<ObjectDisposedException>()
				.Or<OperationCanceledException>()
				.Or<IOException>()
				.Or<InvalidDataException>()
				.RetryAsync(3)
				.ExecuteAsync(async () =>
				{
					tmpExe = Path.Combine(Path.GetTempPath(), filename);

					ReportStatus($"Downloading {Title ?? Url.ToString()}...");

					using (var fs = File.Create(tmpExe))
					using (var response = await http.GetAsync(Url, HttpCompletionOption.ResponseHeadersRead))
					{
						var contentLength = response.Content.Headers.ContentLength;

						using (var download = await response.Content.ReadAsStreamAsync())
						{
							// Short circuit if no length
							if (!contentLength.HasValue)
							{
								await download.CopyToAsync(fs);
								return;
							}

							var buffer = new byte[81920];
							long totalReadLen = 0;
							var readLen = 0;

							var lastPercent = 0;

							while ((readLen = await download.ReadAsync(buffer, 0, buffer.Length, cancelToken)) != 0)
							{
								await fs.WriteAsync(buffer, 0, readLen, cancelToken).ConfigureAwait(false);
								totalReadLen += readLen;

								var percent = (int)(((double)totalReadLen / (double)contentLength) * 100d);

								if (percent % 10 == 0 && percent != lastPercent)
								{
									lastPercent = percent;
									ReportStatus($"Downloading... {percent}%...");
								}
							}
						}
					}

					ReportStatus($"Downloaded {Title ?? Url.ToString()}.");

				});

			var logFile = Path.GetTempFileName();

			ReportStatus($"Installing {Title ?? Url.ToString()}...");

			var p = new ShellProcessRunner(new ShellProcessRunnerOptions(tmpExe, $"/install /quiet /norestart /log \"{logFile}\""));

			var r = p.WaitForExit();

			if (r.ExitCode == 0)
			{
				ReportStatus($"Installed {Title ?? Url.ToString()}.");
			}
			else
			{
				ReportStatus($"Installation failed for {Title ?? Url.ToString()}.  See log file for more details: {logFile}");				
			}
		}
		
	}
}
