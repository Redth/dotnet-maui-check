using Newtonsoft.Json;

namespace DotNetCheck.Manifest
{
	public partial class NuGetPackage
	{
		[JsonProperty("id")]
		public string Id { get; set; }

		[JsonProperty("version")]
		public string Version { get; set; }
	}
}
