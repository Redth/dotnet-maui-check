using System.Collections.Generic;
using Newtonsoft.Json;

namespace DotNetCheck.Manifest
{
	public partial class DotNetSdk
	{
		[JsonProperty("urls")]
		public Urls Urls { get; set; }

		[JsonProperty("version")]
		public string Version { get; set; }

		[JsonProperty("packs")]
		public List<DotNetSdkPack> Packs { get; set; }

		[JsonProperty("workloads")]
		public List<DotNetWorkload> Workloads { get; set; }

		[JsonProperty("packageSources")]
		public List<string> PackageSources { get; set; }

		[JsonProperty("enableWorkloadResolver")]
		public bool EnableWorkloadResolver { get; set; }
	}
}
