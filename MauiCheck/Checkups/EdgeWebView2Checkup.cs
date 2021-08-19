using System.Threading.Tasks;
using DotNetCheck.Models;

namespace DotNetCheck.Checkups
{
    public class EdgeWebView2Checkup : Checkup
	{
		public override bool IsPlatformSupported(Platform platform)
			=> platform == Platform.Windows;

		public override string Id => "edgewebview2";

		public override string Title => $"Edge WebView2";
		
		public override bool ShouldExamine(SharedState history)
			=> Manifest?.Check?.VSWin != null
				&& !(history.GetEnvironmentVariable("CI") ?? "false").Equals("true", System.StringComparison.OrdinalIgnoreCase);

		public override Task<DiagnosticResult> Examine(SharedState history)
		{
			// Info here: https://docs.microsoft.com/en-us/microsoft-edge/webview2/concepts/distribution#online-only-deployment
			string WebView2RegistryKey = @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}";

			var webView2VersionObject = Microsoft.Win32.Registry.GetValue(
                keyName: WebView2RegistryKey,
                valueName: "pv",
                defaultValue: null);

			var webView2Version = webView2VersionObject as string;

			var isWebView2Installed = !string.IsNullOrEmpty(webView2Version);
			if (isWebView2Installed)
            {
				ReportStatus($"Found Edge WebView2 version {webView2Version}", Status.Ok);
                return Task.FromResult(DiagnosticResult.Ok(this));
            }

			return Task.FromResult(new DiagnosticResult(
                Status.Error,
                this,
                new Suggestion($"Download Edge WebView2 from https://developer.microsoft.com/microsoft-edge/webview2/")));
		}
	}
}
