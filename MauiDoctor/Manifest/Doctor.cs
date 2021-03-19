using Newtonsoft.Json;

namespace MauiDoctor.Manifest
{
	public partial class Doctor
	{
		[JsonProperty("toolVersion")]
		public string ToolVersion { get; set; }

		[JsonProperty("openjdk")]
		public MinExactVersion OpenJdk { get; set; }

		[JsonProperty("xcode")]
		public MinExactVersion XCode { get; set; }

		[JsonProperty("vswin")]
		public MinExactVersion VSWin { get; set; }

		[JsonProperty("vsmac")]
		public MinExactVersion VSMac { get; set; }

		[JsonProperty("android")]
		public Android Android { get; set; }

		[JsonProperty("dotnet")]
		public DotNet DotNet { get; set; }
	}
}
