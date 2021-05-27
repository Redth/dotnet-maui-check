using Newtonsoft.Json;

namespace DotNetCheck.Manifest
{
	public class XPathVariableMapping
	{
		[JsonProperty("name")]
		public string Name { get; set; }

		[JsonProperty("xpath")]
		public string XPath { get; set; }
	}
}
