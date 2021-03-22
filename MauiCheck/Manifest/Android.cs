using System.Collections.Generic;
using Newtonsoft.Json;

namespace DotNetCheck.Manifest
{
	public partial class Android
	{
		[JsonProperty("packages")]
		public List<AndroidPackage> Packages { get; set; }

		[JsonProperty("emulators")]
		public List<AndroidEmulator> Emulators { get; set; }
	}
}
