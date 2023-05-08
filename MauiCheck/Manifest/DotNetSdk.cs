using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace DotNetCheck.Manifest
{
	public partial class DotNetSdk
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

		[JsonProperty("packageSources")]
		public List<string> PackageSources { get; set; }

		[JsonProperty("workloadRollback", NullValueHandling = NullValueHandling.Ignore)]
		public System.Uri WorkloadRollback { get; set; }

		[JsonProperty("workloadIds", NullValueHandling = NullValueHandling.Ignore)]
		public List<string> WorkloadIds { get; set; }
	}
}
