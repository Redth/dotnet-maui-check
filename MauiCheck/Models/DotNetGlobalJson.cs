using Newtonsoft.Json;

namespace DotNetCheck.Models
{
	public class DotNetGlobalJson
	{
		[JsonProperty("sdk")]
		public DotNetGlobalJsonSdk Sdk { get; set; } = new DotNetGlobalJsonSdk();

		public string ToJson()
			=> JsonConvert.SerializeObject(this, Formatting.Indented);
	}
}
