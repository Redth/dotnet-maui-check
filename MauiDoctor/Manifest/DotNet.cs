using System.Collections.Generic;
using Newtonsoft.Json;

namespace MauiDoctor.Manifest
{
	public partial class DotNet
	{
		[JsonProperty("sdks")]
		public List<DotNetSdk> Sdks { get; set; }
	}
}
