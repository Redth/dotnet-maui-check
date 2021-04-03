using Newtonsoft.Json;

namespace DotNetCheck.Models
{
	public class DotNetGlobalJsonSdk
	{
		[JsonProperty("version", NullValueHandling = NullValueHandling.Ignore)]
		public string Version { get; set; }

		[JsonProperty("allowPrerelease", NullValueHandling = NullValueHandling.Ignore)]
		public bool? AllowPrerelease { get; set; }

		[JsonProperty("rollForward", NullValueHandling = NullValueHandling.Ignore)]
		public string RollForward { get; set; }

		public static readonly string[] ValidRollForwardValues = new[]
		{
			"patch", "feature", "minor", "major","latestPatch","latestFeature","latestMinor","latestMajor","disable"
		};
	}
}
