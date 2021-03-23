using System;
using Newtonsoft.Json;

namespace DotNetCheck.Manifest
{
	public partial class Urls
	{
		[JsonProperty("win", NullValueHandling = NullValueHandling.Ignore)]
		public Uri Win { get; set; }

		[JsonProperty("win64", NullValueHandling = NullValueHandling.Ignore)]
		public Uri Win64 { get; set; }

		[JsonProperty("osx", NullValueHandling = NullValueHandling.Ignore)]
		public Uri Osx { get; set; }

		[JsonProperty("linux", NullValueHandling = NullValueHandling.Ignore)]
		public Uri Linux { get; set; }

		public Uri ForCurrent()
			=> For(Util.Platform);

		public Uri For(Platform platform)
			=> platform switch
			{
				Platform.OSX => Osx,
				Platform.Windows => Util.Is64 ? Win64 ?? Win : Win,
				Platform.Linux => Linux,
				_ => Win
			};
	}
}
