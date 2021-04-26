using Newtonsoft.Json;
using System.Collections.Generic;

namespace DotNetCheck.Manifest
{
	public partial class Check
	{
		[JsonProperty("toolVersion")]
		public string ToolVersion { get; set; }

		[JsonProperty("openjdk")]
		public OpenJdk OpenJdk { get; set; }

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

		[JsonProperty("filepermissions")]
		public List<FilePermissions> FilePermissions { get; set; }
	}
}
