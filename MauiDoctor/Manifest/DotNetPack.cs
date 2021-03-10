using Newtonsoft.Json;

namespace MauiDoctor.Manifest
{
	public partial class DotNetPack
	{
		[JsonProperty("id")]
		public string Id { get; set; }

		[JsonProperty("version")]
		public string Version { get; set; }

		[JsonProperty("workload")]
		public string Workload { get; set; }

		[JsonProperty("urls")]
		public Urls Urls { get; set; }

		public bool SupportedFor(Platform platform)
			=> Urls?.For(platform) != null;
	}
}
