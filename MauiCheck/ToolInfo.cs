using NuGet.Versioning;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace DotNetCheck
{
	public class ToolInfo
	{
		public const string ToolName = ".NET MAUI Check";
		public const string ToolPackageId = "Redth.Net.Maui.Check";
		public const string ToolCommand = "maui-check";

		public static async Task<Manifest.Manifest> LoadManifest(string fileOrUrl, bool dev)
		{
			var f = fileOrUrl ??
				(dev ? Manifest.Manifest.DevManifestUrl : Manifest.Manifest.DefaultManifestUrl);

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
