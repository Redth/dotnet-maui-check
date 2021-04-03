using Newtonsoft.Json;

namespace DotNetCheck.Models
{
	public class DotNetGlobalJson
	{
		[JsonProperty("sdk")]
		public DotNetGlobalJsonSdk Sdk { get; set; } = new DotNetGlobalJsonSdk();
	}
}
