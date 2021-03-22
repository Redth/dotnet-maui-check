using Spectre.Console.Cli;

namespace DotNetCheck.Cli
{
	public class CheckSettings : CommandSettings
	{
		[CommandOption("-m|--manifest <FILE_OR_URL>")]
		public string Manifest { get; set; } = "https://aka.ms/dotnet-maui-check-manifest";

		[CommandOption("-f|--fix")]
		public bool Fix { get; set; }

		[CommandOption("-n|--non-interactive")]
		public bool NonInteractive { get; set; }
	}
}
