using Spectre.Console.Cli;

namespace DotNetCheck.Cli
{
	public class CheckSettings : CommandSettings
	{
		[CommandOption("-m|--manifest <FILE_OR_URL>")]
		public string Manifest { get; set; }

		[CommandOption("-f|--fix")]
		public bool Fix { get; set; }

		[CommandOption("-n|--non-interactive")]
		public bool NonInteractive { get; set; }

		[CommandOption("-s|--skip <CHECKUP_ID>")]
		public string[] Skip { get; set; }

		[CommandOption("-d|--dev")]
		public bool Dev { get; set; }
	}
}
