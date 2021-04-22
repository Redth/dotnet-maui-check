using Newtonsoft.Json;

namespace DotNetCheck.Manifest
{
	public partial class OpenJdk
	{
		[JsonProperty("urls")]
		public Urls Urls { get; set; }

		[JsonIgnore]
		public System.Uri Url
			=> Urls?.Get(Version);

		[JsonProperty("version")]
		public string Version { get; set; }

		[JsonProperty("requireExact")]
		public bool RequireExact { get; set; }
	}
}
