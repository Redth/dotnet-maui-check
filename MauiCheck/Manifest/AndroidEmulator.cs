using Newtonsoft.Json;
using System.Collections.Generic;

namespace DotNetCheck.Manifest
{
	public partial class AndroidEmulator
	{
		[JsonProperty("desc")]
		public string Description { get; set; }

		[JsonProperty("apiLevel")]
		public int ApiLevel { get; set; }

		[JsonProperty("sdkId")]
		public string SdkId { get; set; }

		[JsonProperty("alternateSdkIds")]
		public List<string> AlternateSdkIds { get; set; }

		[JsonProperty("device")]
		public string Device { get; set; }

		[JsonProperty("tag")]
		public string Tag { get; set; }

		[JsonProperty("arch")]
		public string Arch { get; set; }

		public bool IsArchCompatible()
			=> string.IsNullOrEmpty(Arch) || (Arch.Equals("x86") && !Util.Is64) || (!Arch.Equals("x86") && Util.Is64);
	}
}
