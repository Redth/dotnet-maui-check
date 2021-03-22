using Newtonsoft.Json;

namespace MauiDoctor.Manifest
{
	public partial class AndroidPackage
	{
		[JsonProperty("path")]
		public string Path { get; set; }

		[JsonProperty("version")]
		public string Version { get; set; }

		[JsonProperty("arch")]
		public string Arch { get; set; }

		public bool IsArchCompatible()
			=> string.IsNullOrEmpty(Arch) || (Arch.Equals("x86") && !Util.Is64) || (!Arch.Equals("x86") && Util.Is64);
	}
}
