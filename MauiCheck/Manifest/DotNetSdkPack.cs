using Newtonsoft.Json;

namespace DotNetCheck.Manifest
{
	public partial class DotNetSdkPack : NuGetPackage
	{
		[JsonProperty("skipManifestCheck")]
		public bool SkipManifestCheck { get; set; } = false;

		[JsonProperty("packKind")]
		public string PackKind { get; set; }

		[JsonProperty("templateShortName")]
		public string TemplateShortName { get; set; }
	}
}
