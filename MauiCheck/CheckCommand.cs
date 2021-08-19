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
		public override async Task<int> ExecuteAsync(CommandContext context, CheckSettings settings)
		{
			Util.Verbose = settings.Verbose;
			Util.CI = settings.CI;
			if (settings.CI)
				settings.NonInteractive = true;

			Console.Title = ToolInfo.ToolName;

			AnsiConsole.Render(
				new FigletText(".NET MAUI").LeftAligned().Color(Color.Green));

			AnsiConsole.MarkupLine($"[underline bold green]{Icon.Ambulance} {ToolInfo.ToolName} v{ToolInfo.CurrentVersion} {Icon.Recommend}[/]");
			AnsiConsole.Render(new Rule());

			AnsiConsole.MarkupLine("This tool will attempt to evaluate your .NET MAUI development environment.");
			AnsiConsole.MarkupLine("If problems are detected, this tool may offer the option to try and fix them for you, or suggest a way to fix them yourself.");
			AnsiConsole.WriteLine();
			AnsiConsole.MarkupLine("Thanks for choosing .NET MAUI!");
			AnsiConsole.Render(new Rule());

			if (!Util.IsAdmin() && Util.IsWindows)
			{
				var suTxt = Util.IsWindows ? "Administrator" : "Superuser (su)";

				AnsiConsole.MarkupLine($"[bold red]{Icon.Bell} {suTxt} is required to fix most issues.  Consider exiting and running the tool with {suTxt} permissions.[/]");

				AnsiConsole.Render(new Rule());

				if (!settings.NonInteractive)
				{
					if (!AnsiConsole.Confirm("Would you still like to continue?", false))
						return 1;
				}
			}

			var cts = new System.Threading.CancellationTokenSource();

			var checkupStatus = new Dictionary<string, Models.Status>();
			var sharedState = new SharedState();

			var results = new Dictionary<string, DiagnosticResult>();
			var consoleStatus = AnsiConsole.Status();

			var skippedChecks = new List<string>();

			AnsiConsole.Markup($"[bold blue]{Icon.Thinking} Synchronizing configuration...[/]");

			var channel = ManifestChannel.Default;
			if (settings.Preview)
				channel = ManifestChannel.Preview;
			if (settings.Main)
				channel = ManifestChannel.Main;

			var manifest = await ToolInfo.LoadManifest(settings.Manifest, channel);

			if (!ToolInfo.Validate(manifest))
			{
				ToolInfo.ExitPrompt(settings.NonInteractive);
				return -1;
			}

			AnsiConsole.MarkupLine(" ok");
			AnsiConsole.Markup($"[bold blue]{Icon.Thinking} Scheduling appointments...[/]");

			if (!string.IsNullOrEmpty(settings.DotNetSdkRoot))
			{
				sharedState.SetEnvironmentVariable("DOTNET_ROOT", settings.DotNetSdkRoot);
			}

			if (settings.ForceDotNet)
				sharedState.SetEnvironmentVariable("DOTNET_FORCE", "true");
			if (settings.CI)
				sharedState.SetEnvironmentVariable("CI", "true");

			var checkups = CheckupManager.BuildCheckupGraph(manifest, sharedState);

			AnsiConsole.MarkupLine(" ok");

			var checkupId = string.Empty;

			for (int i = 0; i < checkups.Count(); i++)
			{
				var checkup = checkups.ElementAt(i);

				// Set the manifest
				checkup.Manifest = manifest;

				// If the ID is the same, it's a retry
				var isRetry = checkupId == checkup.Id;

				// Track the last used id so we can detect retry
				checkupId = checkup.Id;

				if (!checkup.ShouldExamine(sharedState))
				{
					checkupStatus[checkup.Id] = Models.Status.Ok;
					continue;
				}

				var skipCheckup = false;

				var dependencies = checkup.DeclareDependencies(checkups.Select(c => c.Id));

				// Make sure our dependencies succeeded first
				if (dependencies?.Any() ?? false)
				{
					foreach (var dep in dependencies)
					{
						var depCheckup = checkups.FirstOrDefault(c => c.Id.StartsWith(dep.CheckupId, StringComparison.OrdinalIgnoreCase));

						if (depCheckup != null && depCheckup.IsPlatformSupported(Util.Platform))
						{
							if (!checkupStatus.TryGetValue(dep.CheckupId, out var depStatus) || depStatus == Models.Status.Error)
							{
								skipCheckup = dep.IsRequired;
								break;
							}
						}
					}
				}

				// See if --skip was specified
				if (settings.Skip?.Any(s => s.Equals(checkup.Id, StringComparison.OrdinalIgnoreCase)
					|| s.Equals(checkup.GetType().Name, StringComparison.OrdinalIgnoreCase)) ?? false)
					skipCheckup = true;

				if (skipCheckup)
				{
					skippedChecks.Add(checkup.Id);
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
					Util.Exception(ex);
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
							try
							{
								remedy.OnStatusUpdated += remedyStatusUpdated;

								AnsiConsole.MarkupLine($"{Icon.Thinking} Attempting to fix: " + checkup.Title);
									
								await remedy.Implement(sharedState, cts.Token);

								didFix = true;
								AnsiConsole.MarkupLine($"[bold]Fix applied.  Checking again...[/]");
							}
							catch (Exception x) when (x is AccessViolationException || x is UnauthorizedAccessException)
							{
								Util.Exception(x);
								AnsiConsole.Markup(adminMsg);
							}
							catch (Exception ex)
							{
								Util.Exception(ex);
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

			var erroredChecks = results.Values.Where(d => d.Status == Models.Status.Error && !skippedChecks.Contains(d.Checkup.Id));

			foreach (var ec in erroredChecks)
				Util.Log($"Checkup had Error status: {ec.Checkup.Id}");

			var hasErrors = erroredChecks.Any();

			var warningChecks = results.Values.Where(d => d.Status == Models.Status.Warning && !skippedChecks.Contains(d.Checkup.Id));
			var hasWarnings = warningChecks.Any();

			if (hasErrors)
			{
				AnsiConsole.Console.WriteLine();

				foreach (var ec in erroredChecks)
					Util.Log($"{ec.Checkup.Id}: {ec.Message}");

				AnsiConsole.MarkupLine($"[bold red]{Icon.Bell} There were one or more problems detected.[/]");
				AnsiConsole.MarkupLine($"[bold red]Please review the errors and correct them and run {ToolInfo.ToolCommand} again.[/]");
			}
			else if (hasWarnings)
			{
				AnsiConsole.Console.WriteLine();

				foreach (var wc in warningChecks)
					Util.Log($"{wc.Checkup.Id}: {wc.Message}");

				AnsiConsole.Console.WriteLine();
				AnsiConsole.MarkupLine($"[bold darkorange3_1]{Icon.Warning} Things look almost great, except some pesky warning(s) which may or may not be a problem, but at least if they are, you'll know where to start searching![/]");
			}
			else
			{
				AnsiConsole.MarkupLine($"[bold blue]{Icon.Success} Congratulations, everything looks great![/]");
			}

			Console.Title = ToolInfo.ToolName;

			ToolInfo.ExitPrompt(settings.NonInteractive);

			Util.Log($"Has Errors? {hasErrors}");
			var exitCode = hasErrors ? 1 : 0;
			Environment.ExitCode = exitCode;

			return exitCode;
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
