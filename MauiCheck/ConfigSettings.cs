using System.Collections.Generic;
using Spectre.Console.Cli;

namespace DotNetCheck
{
	public class ConfigSettings : CommandSettings, IManifestChannelSettings
	{
		[CommandOption("-m|--manifest <FILE_OR_URL>")]
		public string Manifest { get; set; }

		[CommandOption("--pre|--preview|-d|--dev")]
		public bool Preview { get; set; }

		[CommandOption("--main")]
		public bool Main { get; set; }

		[CommandOption("-n|--non-interactive")]
		public bool NonInteractive { get; set; }

		[CommandOption("--nuget|--nuget-sources")]
		public bool NuGetSources { get; set; }

		[CommandOption("--dotnet|--dotnet-version")]
		public bool DotNetVersion { get; set; }

		[CommandOption("--dotnet-pre <VALUE>")]
		public bool? DotNetAllowPrerelease { get; set; }

		[CommandOption("--dotnet-rollforward <OPTION>")]
		public string DotNetRollForward { get; set; }
	}
}