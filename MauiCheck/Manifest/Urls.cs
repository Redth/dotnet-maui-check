using System;
using Newtonsoft.Json;

namespace DotNetCheck.Manifest
{
	public partial class Urls
	{
		[JsonProperty("win", NullValueHandling = NullValueHandling.Ignore)]
		public Uri Win { get; set; }

		[JsonProperty("winArm64", NullValueHandling = NullValueHandling.Ignore)]
		public Uri WinArm64 { get; set; }

		[JsonProperty("win64", NullValueHandling = NullValueHandling.Ignore)]
		public Uri Win64 { get; set; }

		[JsonProperty("osx", NullValueHandling = NullValueHandling.Ignore)]
		public Uri Osx { get; set; }

		[JsonProperty("osxArm64", NullValueHandling = NullValueHandling.Ignore)]
		public Uri OsxArm64 { get; set; }

		[JsonProperty("linux", NullValueHandling = NullValueHandling.Ignore)]
		public Uri Linux { get; set; }

		public Uri Get(string version, Platform? platform = default)
			=> new Uri(For(platform ?? Util.Platform)?.ToString().Replace("__VERSION__", version));

		Uri For(Platform platform)
			=> platform switch
			{
				Platform.OSX =>
					Util.IsArm64
						? (OsxArm64 ?? Osx)
						: Osx,
				Platform.Windows =>
					Util.IsArm64
						? (WinArm64 ?? Win64 ?? Win)
						: (Util.Is64
							? Win64 ?? Win
							: Win),
				Platform.Linux => Linux,
				_ => Win
			};
	}
}
