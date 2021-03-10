using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MauiDoctor.Checks;
using MauiDoctor.Doctoring;
using NuGet.Versioning;
using Spectre.Console;

namespace MauiDoctor
{
	class Program
	{
		static Clinic clinic = new Clinic();

		static async Task Main(string[] args)
		{
			AnsiConsole.MarkupLine("[underline bold green].NET MAUI Doctor[/] Press any key to begin!");

			AnsiConsole.Render(new Rule());

			Console.ReadKey();


			var fileOrUrl = "/Users/redth/Desktop/maui-versions.json";

			var cts = new System.Threading.CancellationTokenSource();

			

			await AnsiConsole.Status().StartAsync("[bold blue]Synchronizing configuration...[/]",
				async ctx =>
			{
				ctx.Spinner(Spinner.Known.Ascii);
				ctx.SpinnerStyle = new Style(Color.DodgerBlue1, decoration: Decoration.Bold);


				var chart = await Manifest.Chart.FromFileOrUrl(fileOrUrl);


				ctx.Status("[bold blue]Scheduling appointments...[/]");


				if (chart.Doctor.Android != null)
				{
					clinic.OfferService(new AndroidSdkManagerCheckup());
					clinic.OfferService(new AndroidSdkLicensesCheckup());
					clinic.OfferService(new AndroidSdkPackagesCheckup(chart.Doctor.Android.Packages.ToArray()));
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


				var appointments = clinic.ScheduleAppointments();


				foreach (var appointment in appointments)
				{
					appointment.OnStatusUpdated += (s, e) =>
					{
						AnsiConsole.MarkupLine(e.Message);
					};

					ctx.Status("[bold blue]Checking:[/] " + appointment.Title);

					var diagnosis = await appointment.Examine();

					if (diagnosis.Status == Doctoring.Status.Ok)
					{
						AnsiConsole.MarkupLine(":check_mark_button: [green]" + appointment.Title + "[/]");
						continue;
					}

					var statusEmoji = diagnosis.Status == Doctoring.Status.Error ? ":cross_mark:" : ":warning:";
					var statusColor = diagnosis.Status == Doctoring.Status.Error ? "red" : "orange3";
					AnsiConsole.MarkupLine(statusEmoji + $" [{statusColor}]" + appointment.Title + "[/]");

					if (diagnosis.HasPrescription)
					{
						AnsiConsole.MarkupLine("  :syringe: [bold blue]Recommendation:[/] " + diagnosis.Prescription.Name);

						if (!string.IsNullOrEmpty(diagnosis.Prescription.Description))
							AnsiConsole.MarkupLine(diagnosis.Prescription.Description);

						if (diagnosis.Prescription.HasRemedy)
						{
							ctx.Status("Waiting for user input...");

							if (AnsiConsole.Confirm("  [bold red]Try to fix automatically? [/]"))
							{
								foreach (var remedy in diagnosis.Prescription.Remedies)
								{
									remedy.OnStatusUpdated += (s, e) =>
									{
										ctx.Status(e.Message);
									};

									ctx.Status("Attempting to fix: " + appointment.Title);

									await remedy.Cure(cts.Token);

									AnsiConsole.MarkupLine("  Fix applied.  Run doctor again to verify.");
								}
							}
						}
					}
				}
			});

		}
	}
}
