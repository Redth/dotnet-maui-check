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

			AnsiConsole.MarkupLine("[underline bold green].NET MAUI Doctor[/] Press any key to begin!");

			AnsiConsole.Render(new Rule());

			Console.ReadKey();

			var clinic = new Clinic();

			var cts = new System.Threading.CancellationTokenSource();

			var checkupStatus = new Dictionary<string, Doctoring.Status>();



			var consoleStatus = AnsiConsole.Status();

			AnsiConsole.Markup("[bold blue]Synchronizing configuration...[/]");

			var chart = await Manifest.Chart.FromFileOrUrl(settings.Manifest);

			AnsiConsole.MarkupLine(" ok");

			AnsiConsole.Markup("[bold blue]Scheduling appointments...[/]");

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
					AnsiConsole.MarkupLine("X [bold red]Skipped: " + checkup.Title + "[/]");
					continue;
				}

				checkup.OnStatusUpdated += (s, e) =>
				{
					AnsiConsole.MarkupLine("  " + e.Message);
				};

				AnsiConsole.MarkupLine(":magnifying_glass_tilted_right: [bold]" + checkup.Title + "[/]...");
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

				// Cache the status for dependencies
				checkupStatus.Add(checkup.Id, diagnosis.Status);

				if (diagnosis.Status == Doctoring.Status.Ok)
					continue;

				var statusEmoji = diagnosis.Status == Doctoring.Status.Error ? ":cross_mark:" : ":warning:";
				var statusColor = diagnosis.Status == Doctoring.Status.Error ? "red" : "orange3";

				var msg = !string.IsNullOrEmpty(diagnosis.Message) ? " - " + diagnosis.Message : string.Empty;

				AnsiConsole.MarkupLine(statusEmoji + $" [{statusColor}]" + checkup.Title + $"{msg}[/]");

				if (diagnosis.HasPrescription)
				{
					AnsiConsole.MarkupLine("  :syringe: [bold blue]Recommendation:[/] " + diagnosis.Prescription.Name);

					if (!string.IsNullOrEmpty(diagnosis.Prescription.Description))
						AnsiConsole.MarkupLine(diagnosis.Prescription.Description);

					if (diagnosis.Prescription.HasRemedy)
					{
						var confirm = AnsiConsole.Confirm("    [bold]Attempt to fix?[/]");

						if (confirm)
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

									AnsiConsole.MarkupLine("Attempting to fix: " + checkup.Title);
									
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
	}
}
