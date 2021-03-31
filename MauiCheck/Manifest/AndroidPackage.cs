using Newtonsoft.Json;
using System.Collections.Generic;

namespace DotNetCheck.Manifest
{
	public partial class AndroidPackage
	{
		[JsonProperty("path")]
		public string Path { get; set; }

		[JsonProperty("alternatives")]
		public List<AndroidPackage> Alternatives { get; set; }

		[JsonProperty("version")]
		public string Version { get; set; }

		[JsonProperty("arch")]
		public string Arch { get; set; }
	}
}
