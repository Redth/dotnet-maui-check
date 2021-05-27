using Spectre.Console.Cli;

namespace DotNetCheck
{
	public class ListCheckupSettings : CommandSettings, IManifestChannelSettings
	{
		[CommandOption("-m|--manifest <FILE_OR_URL>")]
		public string Manifest { get; set; }

		[CommandOption("--pre|--preview|-d|--dev")]
		public bool Preview { get; set; }

		[CommandOption("--main")]
		public bool Main { get; set; }

		[CommandOption("-n|--non-interactive")]
		public bool NonInteractive { get; set; }
	}
}