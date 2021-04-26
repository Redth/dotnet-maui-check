using Newtonsoft.Json;

namespace DotNetCheck.Manifest
{
	public partial class OpenJdk
	{
		[JsonProperty("urls")]
		public Urls Urls { get; set; }

		[JsonIgnore]
		public System.Uri Url
			=> Urls?.Get(CompatVersion);

		[JsonProperty("version")]
		public string Version { get; set; }

		[JsonProperty("minimumVersion")]
		public string MinimumVersion { get; set; }

		[JsonIgnore]
		public string CompatVersion
			=> Version ?? MinimumVersion;

		[JsonProperty("requireExact")]
		public bool RequireExact { get; set; }
	}
}
