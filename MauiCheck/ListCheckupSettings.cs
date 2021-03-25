using Spectre.Console.Cli;

namespace DotNetCheck
{
	public class ListCheckupSettings : CommandSettings
	{
		[CommandOption("-m|--manifest <FILE_OR_URL>")]
		public string Manifest { get; set; }

		[CommandOption("-d|--dev")]
		public bool Dev { get; set; }

		[CommandOption("-n|--non-interactive")]
		public bool NonInteractive { get; set; }
	}
}