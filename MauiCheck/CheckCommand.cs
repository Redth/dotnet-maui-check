using DotNetCheck.Checkups;
using DotNetCheck.Models;
using NuGet.Versioning;
using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNetCheck.Cli
{
	public class CheckCommand : AsyncCommand<CheckSettings>
	{
		const string ToolName = ".NET MAUI Check";
		const string ToolPackageId = "Redth.Net.Maui.Check";
		const string ToolCommand = "maui-check";

		public override async Task<int> ExecuteAsync(CommandContext context, CheckSettings settings)
		{
			Console.Title = ToolName;

			AnsiConsole.MarkupLine($"[underline bold green]{Icon.Ambulance} {ToolName} {Icon.Recommend}[/]");
			AnsiConsole.Render(new Rule());

			AnsiConsole.MarkupLine("This tool will attempt to evaluate your .NET MAUI development environment.");
			AnsiConsole.MarkupLine("If problems are detected, this tool may offer the option to try and fix them for you, or suggest a way to fix them yourself.");
			AnsiConsole.WriteLine();
			AnsiConsole.MarkupLine("Thanks for choosing .NET MAUI!");
			AnsiConsole.Render(new Rule());

			if (!Util.IsAdmin())
			{
				var suTxt = Util.IsWindows ? "Administrator" : "Superuser (su)";

				AnsiConsole.MarkupLine($"[bold red]{Icon.Bell} {suTxt} is required to fix most issues.  Consider exiting and running the tool with {suTxt} permissions.");

				AnsiConsole.Render(new Rule());

				if (!settings.NonInteractive)
				{
					if (!AnsiConsole.Confirm("Would you still like to continue?", false))
						return 1;
				}
			}

			var manager = new CheckupManager();
			var cts = new System.Threading.CancellationTokenSource();

			var checkupStatus = new Dictionary<string, Models.Status>();
			var sharedState = new SharedState();

			var results = new Dictionary<string, DiagnosticResult>();
			var consoleStatus = AnsiConsole.Status();

			AnsiConsole.Markup($"[bold blue]{Icon.Thinking} Synchronizing configuration...[/]");

			var manifest = await Manifest.Manifest.FromFileOrUrl(settings.Manifest);
			var toolVersion = manifest?.Check?.ToolVersion ?? "0.1.0";

			var fileVersion = NuGetVersion.Parse(FileVersionInfo.GetVersionInfo(this.GetType().Assembly.Location).FileVersion);

			if (string.IsNullOrEmpty(toolVersion) || !NuGetVersion.TryParse(toolVersion, out var toolVer) || fileVersion < toolVer)
			{
				Console.WriteLine();
				AnsiConsole.MarkupLine($"[bold red]{Icon.Error} Updating to version {toolVersion} or newer is required:[/]");
				AnsiConsole.MarkupLine($"[red]Update with the following:[/]");

				var installCmdVer = string.IsNullOrEmpty(toolVersion) ? "" : $" --version {toolVersion}";
				AnsiConsole.Markup($"  dotnet tool install --global {ToolPackageId}{installCmdVer}");

				return -1;
			}

			AnsiConsole.MarkupLine(" ok");
			AnsiConsole.Markup($"[bold blue]{Icon.Thinking} Scheduling appointments...[/]");

			if (manifest.Check.OpenJdk != null)
			{
				manager.ContributeDiagnostic(new OpenJdkCheckup(manifest.Check.OpenJdk.MinimumVersion, manifest.Check.OpenJdk.ExactVersion));
			}

			if (manifest.Check.Android != null)
			{
				manager.ContributeDiagnostic(new XAndroidSdkPackagesCheckup(manifest.Check.Android.Packages.ToArray()));
			
				if (manifest.Check.Android.Emulators?.Any() ?? false)
					manager.ContributeDiagnostic(new AndroidEmulatorCheckup(manifest.Check.Android.Emulators.ToArray()));
			}

			if (manifest.Check.XCode != null)
				manager.ContributeDiagnostic(new XCodeCheckup(manifest.Check.XCode.MinimumVersion, manifest.Check.XCode.ExactVersion));

			if (Util.IsMac && manifest.Check.VSMac != null && !string.IsNullOrEmpty(manifest.Check.VSMac.MinimumVersion))
				manager.ContributeDiagnostic(new VisualStudioMacCheckup(manifest.Check.VSMac.MinimumVersion, manifest.Check.VSMac.ExactVersion));

			if (Util.IsWindows && manifest.Check.VSWin != null && !string.IsNullOrEmpty(manifest.Check.VSWin.MinimumVersion))
				manager.ContributeDiagnostic(new VisualStudioWindowsCheckup(manifest.Check.VSWin.MinimumVersion, manifest.Check.VSWin.ExactVersion));


			if (manifest.Check.DotNet?.Sdks?.Any() ?? false)
			{
				manager.ContributeDiagnostic(new DotNetCheckup(manifest.Check.DotNet.Sdks.ToArray()));

				foreach (var sdk in manifest.Check.DotNet.Sdks)
				{
					if (sdk.Workloads?.Any() ?? false)
						manager.ContributeDiagnostic(new DotNetWorkloadsCheckup(sdk.Version, sdk.Workloads.ToArray(), sdk.PackageSources.ToArray()));

					// Always run the packs checkup even if manifest is empty, since the workloads may resolve some required packs dynamically that aren't from the manifest
					manager.ContributeDiagnostic(new DotNetPacksCheckup(sdk.Version, sdk.Packs?.ToArray() ?? Array.Empty<Manifest.DotNetSdkPack>(), sdk.PackageSources.ToArray()));
				}

				manager.ContributeDiagnostic(new DotNetSentinelCheckup());
			}


			var checkups = manager.BuildCheckupGraph();

			AnsiConsole.MarkupLine(" ok");

			var checkupId = string.Empty;

			for (int i = 0; i < checkups.Count(); i++)
			{
				var checkup = checkups.ElementAt(i);

				// If the ID is the same, it's a retry
				var isRetry = checkupId == checkup.Id;

				// Track the last used id so we can detect retry
				checkupId = checkup.Id;

				var skipCheckup = false;

				// Make sure our dependencies succeeded first
				if (checkup.Dependencies?.Any() ?? false)
				{
					foreach (var dep in checkup.Dependencies)
					{
						if (!checkupStatus.TryGetValue(dep.CheckupId, out var depStatus) || depStatus == Models.Status.Error)
						{
							skipCheckup = dep.IsRequired;
							break;
						}
					}
				}

				if (skipCheckup)
				{
					checkupStatus[checkup.Id] = Models.Status.Error;
					AnsiConsole.WriteLine();
					AnsiConsole.MarkupLine($"[bold red]{Icon.Error} Skipped: " + checkup.Title + "[/]");
					continue;
				}

				checkup.OnStatusUpdated += checkupStatusUpdated;

				AnsiConsole.WriteLine();
				AnsiConsole.MarkupLine($"[bold]{Icon.Checking} " + checkup.Title + " Checkup[/]...");
				Console.Title = checkup.Title;

				DiagnosticResult diagnosis = null;

				try
				{
					diagnosis = await checkup.Examine(sharedState);
				}
				catch (Exception ex)
				{
					diagnosis = new DiagnosticResult(Models.Status.Error, checkup, ex.Message);
				}

				results[checkup.Id] = diagnosis;

				// Cache the status for dependencies
				checkupStatus[checkup.Id] = diagnosis.Status;

				if (diagnosis.Status == Models.Status.Ok)
					continue;

				var statusEmoji = diagnosis.Status == Models.Status.Error ? Icon.Error : Icon.Warning;
				var statusColor = diagnosis.Status == Models.Status.Error ? "red" : "darkorange3_1";

				var msg = !string.IsNullOrEmpty(diagnosis.Message) ? " - " + diagnosis.Message : string.Empty;

				if (diagnosis.HasSuggestion)
				{
					Console.WriteLine();
					AnsiConsole.Render(new Rule());
					AnsiConsole.MarkupLine($"[bold blue]{Icon.Recommend} Recommendation:[/][blue] {diagnosis.Suggestion.Name}[/]");

					if (!string.IsNullOrEmpty(diagnosis.Suggestion.Description))
						AnsiConsole.MarkupLine("" + diagnosis.Suggestion.Description + "");

					AnsiConsole.Render(new Rule());
					Console.WriteLine();

					// See if we should fix
					// needs to have a remedy available to even bother asking/trying
					var doFix = diagnosis.Suggestion.HasSolution
						&& (
							// --fix + --non-interactive == auto fix, no prompt
							(settings.NonInteractive && settings.Fix)
							// interactive (default) + prompt/confirm they want to fix
							|| (!settings.NonInteractive && AnsiConsole.Confirm($"[bold]{Icon.Bell} Attempt to fix?[/]"))
						);

					if (doFix && !isRetry)
					{
						var isAdmin = Util.IsAdmin();

						var adminMsg = Util.IsWindows ?
							$"{Icon.Bell} [red]Administrator Permissions Required.  Try opening a new console as Administrator and running this tool again.[/]"
							: $"{Icon.Bell} [red]Super User Permissions Required.  Try running this tool again with 'sudo'.[/]";

						var didFix = false;

						foreach (var remedy in diagnosis.Suggestion.Solutions)
						{
							if (!remedy.HasPrivilegesToRun(isAdmin, Util.Platform))
							{
								AnsiConsole.Markup(adminMsg);
								continue;
							}
							try
							{
								remedy.OnStatusUpdated += remedyStatusUpdated;

								AnsiConsole.MarkupLine($"{Icon.Thinking} Attempting to fix: " + checkup.Title);
									
								await remedy.Implement(cts.Token);

								didFix = true;
								AnsiConsole.MarkupLine($"[bold]Fix applied.  Checking again...[/]");
							}
							catch (Exception x) when (x is AccessViolationException || x is UnauthorizedAccessException)
							{
								AnsiConsole.Markup(adminMsg);
							}
							catch (Exception ex)
							{
								AnsiConsole.MarkupLine("[bold red]Fix failed - " + ex.Message + "[/]");
							}
							finally
							{
								remedy.OnStatusUpdated -= remedyStatusUpdated;
							}
						}

						// RETRY The check again
						if (didFix)
							i--;
					}
				}

				checkup.OnStatusUpdated -= checkupStatusUpdated;
			}

			AnsiConsole.Render(new Rule());
			AnsiConsole.WriteLine();

			var hasErrors = results.Values.Any(d => d.Status == Models.Status.Error);

			if (hasErrors)
			{
				AnsiConsole.MarkupLine($"[bold red]{Icon.Bell} There were one or more problems detected.[/]");
				AnsiConsole.MarkupLine($"[bold red]Please review the errors and correct them and run {ToolCommand} again.[/]");
			}
			else
			{
				AnsiConsole.MarkupLine($"[bold blue]{Icon.Success} Congratulations, everything looks great![/]");
			}

			Console.Title = ToolName;

			if (!settings.NonInteractive)
			{
				AnsiConsole.MarkupLine("Press return to exit...");
				Console.ReadLine();
			}

			return hasErrors ? 1 : 0;
		}

		void checkupStatusUpdated(object sender, CheckupStatusEventArgs e)
		{
			var msg = "";
			if (e.Status == Models.Status.Error)
				msg = $"[red]{Icon.Error} {e.Message}[/]";
			else if (e.Status == Models.Status.Warning)
				msg = $"[darkorange3_1]{Icon.Warning} {e.Message}[/]";
			else if (e.Status == Models.Status.Ok)
				msg = $"[green]{Icon.Success} {e.Message}[/]";
			else
				msg = $"{Icon.ListItem} {e.Message}";

			AnsiConsole.MarkupLine("  " + msg);
		}

		void remedyStatusUpdated(object sender, RemedyStatusEventArgs e)
		{
			AnsiConsole.MarkupLine("  " + e.Message);
		}
	}
}
