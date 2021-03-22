using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace DotNetCheck.Manifest
{

	public partial class MinExactVersion
	{
		[JsonProperty("minimumVersion")]
		public string MinimumVersion { get; set; }

		[JsonProperty("exactVersion")]
		public string ExactVersion { get; set; }
	}
}
