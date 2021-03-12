using MauiDoctor.Checkups;
using MauiDoctor.Doctoring;
using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MauiDoctor.Cli
{
	public class DoctorCommand : AsyncCommand<DoctorSettings>
	{
		public override async Task<int> ExecuteAsync(CommandContext context, DoctorSettings settings)
		{
			Console.Title = ".NET MAUI Doctor";

			AnsiConsole.MarkupLine($"[underline bold green]{Icon.Ambulance} .NET MAUI Doctor {Icon.Recommend}[/]");

			AnsiConsole.Render(new Rule());

			AnsiConsole.MarkupLine("This tool will attempt to evaluate your .NET MAUI development environment.");
			AnsiConsole.MarkupLine("If problems are detected, this tool may offer the option to try and fix them for you, or suggest a way to fix them yourself.");
			AnsiConsole.WriteLine();
			AnsiConsole.MarkupLine("Thanks for choosing .NET MAUI!");

			AnsiConsole.WriteLine();

			if (!settings.NonInteractive)
			{
				AnsiConsole.Markup("Press any key to start...");
				Console.ReadKey();
				AnsiConsole.WriteLine();
			}

			AnsiConsole.Render(new Rule());

			

			var clinic = new Clinic();

			var cts = new System.Threading.CancellationTokenSource();

			var checkupStatus = new Dictionary<string, Doctoring.Status>();
			var diagnoses = new List<Diagonosis>();


			var consoleStatus = AnsiConsole.Status();

			AnsiConsole.Markup($"[bold blue]{Icon.Thinking} Synchronizing configuration...[/]");

			var chart = await Manifest.Chart.FromFileOrUrl(settings.Manifest);

			AnsiConsole.MarkupLine(" ok");

			AnsiConsole.Markup($"[bold blue]{Icon.Thinking} Scheduling appointments...[/]");

			if (chart.Doctor.Android != null)
			{
				clinic.OfferService(new AndroidSdkManagerCheckup());
				clinic.OfferService(new AndroidSdkPackagesCheckup(chart.Doctor.Android.Packages.ToArray()));
				clinic.OfferService(new AndroidSdkLicensesCheckup());
			}

			if (chart.Doctor.DotNet != null)
				clinic.OfferService(new XCodeCheckup(chart.Doctor.XCode.MinimumVersion, chart.Doctor.XCode.ExactVersion));

			if (chart.Doctor.VSMac != null && !string.IsNullOrEmpty(chart.Doctor.VSMac.MinimumVersion))
				clinic.OfferService(new VisualStudioMacCheckup(chart.Doctor.VSMac.MinimumVersion, chart.Doctor.VSMac.ExactVersion));

			if (chart.Doctor.VSWin != null && !string.IsNullOrEmpty(chart.Doctor.VSWin.MinimumVersion))
				clinic.OfferService(new VisualStudioWindowsCheckup(chart.Doctor.VSWin.MinimumVersion, chart.Doctor.VSWin.ExactVersion));


			if (chart.Doctor.DotNet?.Sdks?.Any() ?? false)
			{
				clinic.OfferService(new DotNetCheckup(chart.Doctor.DotNet.Sdks.ToArray()));

				foreach (var sdk in chart.Doctor.DotNet.Sdks)
				{
					clinic.OfferService(new DotNetPacksCheckup(sdk.Version, sdk.Packs.ToArray()));
				}
			}


			var checkups = clinic.ScheduleCheckups();

			AnsiConsole.MarkupLine(" ok");

			foreach (var checkup in checkups)
			{
				var skipCheckup = false;

				// Make sure our dependencies succeeded first
				if (checkup.Dependencies?.Any() ?? false)
				{
					foreach (var dep in checkup.Dependencies)
					{
						if (!checkupStatus.TryGetValue(dep, out var depStatus) || depStatus != Doctoring.Status.Ok)
						{
							skipCheckup = true;
							break;
						}
					}
				}

				if (skipCheckup)
				{
					checkupStatus.Add(checkup.Id, Doctoring.Status.Error);
					AnsiConsole.WriteLine();
					AnsiConsole.MarkupLine($"[bold red]{Icon.Error} Skipped: " + checkup.Title + "[/]");
					continue;
				}

				checkup.OnStatusUpdated += (s, e) =>
				{
					var msg = "";
					if (e.Status == Doctoring.Status.Error)
						msg = $"[red]{Icon.Error} {e.Message}[/]";
					else if (e.Status == Doctoring.Status.Warning)
						msg = $"[red]{Icon.Error} {e.Message}[/]";
					else if (e.Status == Doctoring.Status.Ok)
						msg = $"[green]{Icon.Success} {e.Message}[/]";
					else
						msg = $"{Icon.ListItem} {e.Message}";

					AnsiConsole.MarkupLine("  " + msg);
				};

				AnsiConsole.WriteLine();
				AnsiConsole.MarkupLine($"[bold]{Icon.Checking} " + checkup.Title + " Checkup[/]...");
				Console.Title = checkup.Title;

				Diagonosis diagnosis = null;

				try
				{
					diagnosis = await checkup.Examine();
				}
				catch (Exception ex)
				{
					diagnosis = new Diagonosis(Doctoring.Status.Error, checkup, ex.Message);
				}

				diagnoses.Add(diagnosis);

				// Cache the status for dependencies
				checkupStatus.Add(checkup.Id, diagnosis.Status);

				if (diagnosis.Status == Doctoring.Status.Ok)
					continue;

				var statusEmoji = diagnosis.Status == Doctoring.Status.Error ? Icon.Error : Icon.Warning;
				var statusColor = diagnosis.Status == Doctoring.Status.Error ? "red" : "orange3";

				var msg = !string.IsNullOrEmpty(diagnosis.Message) ? " - " + diagnosis.Message : string.Empty;

				//AnsiConsole.MarkupLine($"[{statusColor}]{statusEmoji} {checkup.Title}{msg}[/]");

				if (diagnosis.HasPrescription)
				{
					AnsiConsole.MarkupLine($"  [bold blue]{Icon.Recommend} Recommendation:[/] {diagnosis.Prescription.Name}");

					if (!string.IsNullOrEmpty(diagnosis.Prescription.Description))
						AnsiConsole.MarkupLine("  " + diagnosis.Prescription.Description);

					// See if we should fix
					// needs to have a remedy available to even bother asking/trying
					var doFix = diagnosis.Prescription.HasRemedy
						&& (
							// --fix + --non-interactive == auto fix, no prompt
							(settings.NonInteractive && settings.Fix)
							// interactive (default) + prompt/confirm they want to fix
							|| (!settings.NonInteractive && AnsiConsole.Confirm($"    [bold]{Icon.Bell} Attempt to fix?[/]"))
						);

					if (doFix)
					{
						var isAdmin = Util.IsAdmin();

						foreach (var remedy in diagnosis.Prescription.Remedies)
						{
							if (!remedy.HasPrivilegesToRun(isAdmin, Util.Platform))
							{
								AnsiConsole.Markup("Fix requires running with adminstrator privileges.  Try opening a terminal as administrator and running maui-doctor again.");
								continue;
							}
							try
							{
								remedy.OnStatusUpdated += (s, e) =>
								{
									AnsiConsole.MarkupLine("  " + e.Message);
								};

								AnsiConsole.MarkupLine($"{Icon.Thinking} Attempting to fix: " + checkup.Title);
									
								await remedy.Cure(cts.Token);

								AnsiConsole.MarkupLine("  Fix applied.  Run doctor again to verify.");
							}
							catch (Exception ex)
							{
								AnsiConsole.MarkupLine("  Fix failed - " + ex.Message);
							}
						}
					}
				}
			}

			AnsiConsole.Render(new Rule());
			AnsiConsole.WriteLine();


			if (diagnoses.Any(d => d.Status == Doctoring.Status.Error))
			{
				AnsiConsole.MarkupLine($"[bold red]{Icon.Bell} There were one or more problems detected.[/]");
				AnsiConsole.MarkupLine($"[bold red]Please review the errors and correct them and run maui-doctor again.[/]");
			}
			else
			{
				AnsiConsole.MarkupLine($"[bold blue]{Icon.Success} Congratulations, everything looks great![/]");
			}

			Console.Title = ".NET MAUI Doctor";

			return 0;
		}
	}

	public class DoctorSettings : CommandSettings
	{
		[CommandOption("-m|--manifest <FILE_OR_URL>")]
		public string Manifest { get; set; } = "https://aka.ms/dotnet-maui-doctor-manifest";

		[CommandOption("-f|--fix")]
		public bool Fix { get; set; }

		[CommandOption("-n|--non-interactive")]
		public bool NonInteractive { get; set; }
	}
}
