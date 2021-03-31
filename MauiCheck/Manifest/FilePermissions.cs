using Newtonsoft.Json;
using System.Collections.Generic;

namespace DotNetCheck.Manifest
{
	public class FilePermissions
	{
		[JsonProperty("arch")]
		public string Arch { get; set; }

		[JsonProperty("patterns")]
		public List<string> Patterns { get; set; }

		[JsonProperty("execute")]
		public bool Execute { get; set; }

		public bool IsCompatible()
		{
			var platform = Util.Platform;
			var arch = Arch?.ToLowerInvariant();

			if (string.IsNullOrWhiteSpace(arch))
				return true;

			return arch switch
			{
				"osx" => platform == Platform.OSX,
				"osxArm64" => platform == Platform.OSX && Util.IsArm64,
				"win" => platform == Platform.Windows,
				"winArm64" => platform == Platform.Windows && Util.IsArm64,
				"win64" => platform == Platform.Windows && Util.Is64,
				"linux" => platform == Platform.Linux,
				_ => false
			};
		}
	}
}
