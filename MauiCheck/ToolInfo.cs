using NuGet.Versioning;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace DotNetCheck
{
	public enum ManifestChannel
	{
		Default,
		Preview,
		Main
	}

	public class ToolInfo
	{
		public const string ToolName = ".NET MAUI Check";
		public const string ToolPackageId = "Redth.Net.Maui.Check";
		public const string ToolCommand = "maui-check";

		public static async Task<Manifest.Manifest> LoadManifest(string fileOrUrl, ManifestChannel channel)
		{
			var f = fileOrUrl ??
				channel switch
				{
					ManifestChannel.Preview => Manifest.Manifest.PreviewManifestUrl,
					ManifestChannel.Main => Manifest.Manifest.MainManifestUrl,
					ManifestChannel.Default => Manifest.Manifest.DefaultManifestUrl,
					_ => Manifest.Manifest.DefaultManifestUrl
				};

			Util.Log($"Loading Manifest from: {f}");

			return await Manifest.Manifest.FromFileOrUrl(f);
		}

		public static bool Validate(Manifest.Manifest manifest)
		{
			var toolVersion = manifest?.Check?.ToolVersion ?? "0.1.0";

			Util.Log($"Required Version: {toolVersion}");

			var fileVersion = NuGetVersion.Parse(FileVersionInfo.GetVersionInfo(typeof(ToolInfo).Assembly.Location).FileVersion);

			Util.Log($"Current Version: {fileVersion}");

			if (string.IsNullOrEmpty(toolVersion) || !NuGetVersion.TryParse(toolVersion, out var toolVer) || fileVersion < toolVer)
			{
				Console.WriteLine();
				AnsiConsole.MarkupLine($"[bold red]{Icon.Error} Updating to version {toolVersion} or newer is required:[/]");
				AnsiConsole.MarkupLine($"[blue]  dotnet tool update --global {ToolPackageId}[/]");

				if (System.Diagnostics.Debugger.IsAttached)
				{
					if (AnsiConsole.Confirm("Mismatched version, continue debugging anyway?"))
						return true;
				}

				return false;
			}

			return true;
		}

		public static void ExitPrompt(bool nonInteractive)
		{
			if (!nonInteractive)
			{
				AnsiConsole.WriteLine();
				AnsiConsole.WriteLine("Press enter to exit...");
				Console.ReadLine();
			}
		}
	}
}
