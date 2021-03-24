using NuGet.Versioning;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace DotNetCheck
{
	public class ToolInfo
	{
		public const string ToolName = ".NET MAUI Check";
		public const string ToolPackageId = "Redth.Net.Maui.Check";
		public const string ToolCommand = "maui-check";

		public static bool Validate(Manifest.Manifest manifest)
		{
			var toolVersion = manifest?.Check?.ToolVersion ?? "0.1.0";

			var fileVersion = NuGetVersion.Parse(FileVersionInfo.GetVersionInfo(typeof(ToolInfo).Assembly.Location).FileVersion);

			if (string.IsNullOrEmpty(toolVersion) || !NuGetVersion.TryParse(toolVersion, out var toolVer) || fileVersion < toolVer)
			{
				Console.WriteLine();
				AnsiConsole.MarkupLine($"[bold red]{Icon.Error} Updating to version {toolVersion} or newer is required:[/]");
				AnsiConsole.MarkupLine($"[red]Update with the following:[/]");

				var installCmdVer = string.IsNullOrEmpty(toolVersion) ? "" : $" --version {toolVersion}";
				AnsiConsole.Markup($"  dotnet tool install --global {ToolPackageId}{installCmdVer}");

				return false;
			}

			return true;
		}
	}
}
