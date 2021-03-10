using System.Collections.Generic;
using Newtonsoft.Json;

namespace MauiDoctor.Manifest
{
	public partial class DotNetSdk
	{
		[JsonProperty("urls")]
		public Urls Urls { get; set; }

		[JsonProperty("version")]
		public string Version { get; set; }

		[JsonProperty("packs")]
		public List<DotNetPack> Packs { get; set; }
	}
}
