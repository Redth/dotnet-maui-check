using Spectre.Console.Cli;

namespace DotNetCheck
{
	public class ListCheckupSettings : CommandSettings
	{
		[CommandOption("-m|--manifest <FILE_OR_URL>")]
		public string Manifest { get; set; } = global::DotNetCheck.Manifest.Manifest.DefaultManifestUrl;


		[CommandOption("-n|--non-interactive")]
		public bool NonInteractive { get; set; }
	}
}