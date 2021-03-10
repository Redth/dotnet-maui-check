using System;
using Newtonsoft.Json;

namespace MauiDoctor.Manifest
{
	public partial class Urls
	{
		[JsonProperty("win", NullValueHandling = NullValueHandling.Ignore)]
		public Uri Win { get; set; }

		[JsonProperty("osx", NullValueHandling = NullValueHandling.Ignore)]
		public Uri Osx { get; set; }

		[JsonProperty("linux", NullValueHandling = NullValueHandling.Ignore)]
		public Uri Linux { get; set; }

		public Uri For(Platform platform)
			=> platform switch
			{
				Platform.OSX => Osx,
				Platform.Windows => Win,
				Platform.Linux => Linux,
				_ => Win
			};
	}
}
