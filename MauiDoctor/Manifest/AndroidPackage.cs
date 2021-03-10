using Newtonsoft.Json;

namespace MauiDoctor.Manifest
{
	public partial class AndroidPackage
	{
		[JsonProperty("path")]
		public string Path { get; set; }

		[JsonProperty("version")]
		public string Version { get; set; }
	}
}
