using DotNetCheck.Models;
using Newtonsoft.Json;
using NuGet.Configuration;
using NuGet.Versioning;
using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNetCheck
{
	public class ConfigCommand : AsyncCommand<ConfigSettings>
	{
		public override async Task<int> ExecuteAsync(CommandContext context, ConfigSettings settings)
		{
			AnsiConsole.Markup($"[bold blue]{Icon.Thinking} Synchronizing configuration...[/]");

			var manifest = await ToolInfo.LoadManifest(settings.Manifest, settings.Dev);

			if (!ToolInfo.Validate(manifest))
			{
				ToolInfo.ExitPrompt(settings.NonInteractive);
				return -1;
			}

			AnsiConsole.MarkupLine(" ok");

			var manifestDotNetSdk = manifest?.Check?.DotNet?.Sdks?.FirstOrDefault();
			var manifestNuGetSources = manifestDotNetSdk?.PackageSources;

			if (manifestDotNetSdk != null
				&& (settings.DotNetAllowPrerelease.HasValue
				|| !string.IsNullOrEmpty(settings.DotNetRollForward)
				|| settings.DotNetVersion))
			{
				// Write global.json or update global.json
				/*
				 * 
				 *	{
				 *		"sdk": {
				 *		"version" : "",
				 *		"allowPrerelease": true,
				 *		"rollForward": "patch|feature|minor|major|latestPatch|latestFeature|latestMinor|latestMajor|disable"
				 *		}
				 *	}
				 */

				const string globalJsonFile = "global.json";


				DotNetGlobalJson globaljson = default;

				if (File.Exists(globalJsonFile))
				{
					try { globaljson = JsonConvert.DeserializeObject<DotNetGlobalJson>(File.ReadAllText(globalJsonFile)); }
					catch { }
				}

				if (globaljson == null)
					globaljson = new DotNetGlobalJson();

				if (settings.DotNetVersion)
				{
					Util.Log($"Setting version in global.json: {manifestDotNetSdk.Version}");
					globaljson.Sdk.Version = manifestDotNetSdk.Version;
					AnsiConsole.MarkupLine($"[green]{Icon.Success} Set global.json 'version': {manifestDotNetSdk.Version}[/]");
				}

				if (settings?.DotNetAllowPrerelease.HasValue ?? false)
				{
					Util.Log($"Setting allowPrerelease in global.json: {settings.DotNetAllowPrerelease.Value}");
					globaljson.Sdk.AllowPrerelease = settings.DotNetAllowPrerelease.Value;
					AnsiConsole.MarkupLine($"[green]{Icon.Success} Set global.json 'allowPrerelease': {settings.DotNetAllowPrerelease.Value}[/]");
				}

				if (!string.IsNullOrEmpty(settings.DotNetRollForward))
				{
					if (!DotNetGlobalJsonSdk.ValidRollForwardValues.Contains(settings.DotNetRollForward))
						throw new ArgumentException("Invalid rollForward value specified.  Must be one of: ", string.Join(", ", DotNetGlobalJsonSdk.ValidRollForwardValues));

					Util.Log($"Setting rollForward in global.json: {settings.DotNetRollForward}");
					globaljson.Sdk.RollForward = settings.DotNetRollForward;
					AnsiConsole.MarkupLine($"[green]{Icon.Success} Set global.json 'rollForward': {settings.DotNetRollForward}[/]");
				}

				File.WriteAllText(globalJsonFile, JsonConvert.SerializeObject(globaljson, Formatting.Indented));
			}

			if ((manifestNuGetSources?.Any() ?? false) && settings.NuGetSources)
			{
				// write nuget.config or update
				var nugetConfigFile = "NuGet.config";
				var nugetRoot = Directory.GetCurrentDirectory();

				ISettings ns = new Settings(nugetRoot, nugetConfigFile);

				if (File.Exists(Path.Combine(nugetRoot, nugetConfigFile)))
				{
					try
					{
						ns = Settings.LoadSpecificSettings(nugetRoot, nugetConfigFile);
					}
					catch { }
				}

				var packageSourceProvider = new PackageSourceProvider(ns);

				var addedAny = false;

				foreach (var src in manifestNuGetSources)
				{
					var srcExists = false;
					try
					{
						var existingSrc = packageSourceProvider.GetPackageSourceBySource(src);

						if (existingSrc != null)
							srcExists = true;
					}
					catch { }

					if (srcExists)
					{
						Util.Log($"PackageSource already exists in NuGet.config: {src}");
						AnsiConsole.MarkupLine($"{Icon.ListItem} PackageSource exists in NuGet.config: {src}");
					}
					else
					{
						Util.Log($"Adding PackageSource to NuGet.config: {src}");
						packageSourceProvider.AddPackageSource(new PackageSource(src));
						addedAny = true;

						AnsiConsole.MarkupLine($"[green]{Icon.Success} Added PackageSource to NuGet.config: {src}[/]");
					}
				}

				if (addedAny)
				{
					Util.Log($"Saving NuGet.config");
					ns.SaveToDisk();
				}
			}

			ToolInfo.ExitPrompt(settings.NonInteractive);
			return 0;
		}

	}
}
