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


			var fileOrUrl = "https://aka.ms/dotnet-maui-doctor-manifest";

			var cts = new System.Threading.CancellationTokenSource();

			

			await AnsiConsole.Status().StartAsync("[bold blue]Synchronizing configuration...[/]",
				async ctx =>
			{
				ctx.Spinner(Spinner.Known.Ascii);
				ctx.SpinnerStyle = new Style(Color.DodgerBlue1, decoration: Decoration.Bold);

				var chart = await Manifest.Chart.FromFileOrUrl(fileOrUrl);

				ctx.Status("[bold blue]Scheduling appointments...[/]");

				//if (chart.Doctor.Android != null)
				//{
				//	clinic.OfferService(new AndroidSdkManagerCheckup());
				//	clinic.OfferService(new AndroidSdkLicensesCheckup());
				//	clinic.OfferService(new AndroidSdkPackagesCheckup(chart.Doctor.Android.Packages.ToArray()));
				//}

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


				foreach (var checkup in checkups)
				{
					checkup.OnStatusUpdated += (s, e) =>
					{
						AnsiConsole.MarkupLine(e.Message);
					};

					AnsiConsole.MarkupLine(":magnifying_glass_tilted_right: [bold]" + checkup.Title + "[/]");

					ctx.Status("[bold blue]Checking:[/] " + checkup.Title);

					Diagonosis diagnosis = null;

					try
					{
						diagnosis = await checkup.Examine();
					}
					catch (Exception ex)
					{
						diagnosis = new Diagonosis(Doctoring.Status.Error, checkup, ex.Message);
					}

					if (diagnosis.Status == Doctoring.Status.Ok)
						continue;

					var statusEmoji = diagnosis.Status == Doctoring.Status.Error ? ":cross_mark:" : ":warning:";
					var statusColor = diagnosis.Status == Doctoring.Status.Error ? "red" : "orange3";
					AnsiConsole.MarkupLine(statusEmoji + $" [{statusColor}]" + checkup.Title + "[/]");

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
									try
									{
										remedy.OnStatusUpdated += (s, e) =>
										{
											ctx.Status(e.Message);
										};

										ctx.Status("Attempting to fix: " + checkup.Title);

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
			});

		}
	}
}
