using System.IO;

namespace MauiDoctor.AndroidSdk
{
	public partial class SdkManager
	{
		/// <summary>
		/// Android SDK Manager tool settings.
		/// </summary>
		public class SdkManagerProxyOptions
		{
			/// <summary>
			/// Gets or sets a value indicating whether HTTPS should be used.
			/// </summary>
			/// <value><c>true</c> if no HTTPS; otherwise, <c>false</c>.</value>
			public bool NoHttps { get; set; } = false;

			/// <summary>
			/// Gets or sets the type of the proxy to be used.
			/// </summary>
			/// <value>The type of the proxy.</value>
			public SdkManagerProxyType ProxyType { get; set; } = SdkManagerProxyType.None;

			/// <summary>
			/// Gets or sets the proxy host.
			/// </summary>
			/// <value>The proxy host.</value>
			public string ProxyHost { get; set; }

			/// <summary>
			/// Gets or sets the proxy port.
			/// </summary>
			/// <value>The proxy port.</value>
			public int ProxyPort { get; set; } = -1;

			/// <summary>
			/// Gets or sets a value indicating whether to skip the sdkmanager version check.
			/// By default, the sdkmanager version is checked before each invocation to ensure a new enough version is in use.
			/// </summary>
			/// <value><c>true</c> if skip version check; otherwise, <c>false</c>.</value>
			public bool SkipVersionCheck { get; set; } = false;
		}

		/// <summary>
		/// Android SDK Manager proxy type.
		/// </summary>
		public enum SdkManagerProxyType
		{
			/// <summary>
			/// Do not use a proxy.
			/// </summary>
			None,
			/// <summary>
			/// Use a HTTP proxy.
			/// </summary>
			Http,
			/// <summary>
			/// Use a SOCKS proxy.
			/// </summary>
			Socks
		}

		/// <summary>
		/// Android SDK Manager release channel.
		/// </summary>
		public enum SdkChannel
		{
			/// <summary>
			/// Stable packages.
			/// </summary>
			Stable = 0,
			/// <summary>
			/// Beta packages.
			/// </summary>
			Beta = 1,
			/// <summary>
			/// Developer packages.
			/// </summary>
			Dev = 2,
			/// <summary>
			/// Canary packages.
			/// </summary>
			Canary = 3,
		}
	}
}