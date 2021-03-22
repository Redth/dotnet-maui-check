using Newtonsoft.Json;

namespace MauiDoctor.Manifest
{
	public partial class AndroidEmulator
	{
		[JsonProperty("desc")]
		public string Description { get; set; }

		[JsonProperty("apiLevel")]
		public int ApiLevel { get; set; }

		[JsonProperty("sdkId")]
		public string SdkId { get; set; }
	}
}
