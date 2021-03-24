using DotNetCheck.Models;
using DotNetCheck.Solutions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNetCheck.Checkups
{
	public class DotNetSentinelCheckup : Checkup
	{
		public override string Id => "dotnetsentinel";

		public override string Title => $".NET SDK - EnableWorkloadResolver.sentinel";

		IEnumerable<string> GetAllSentinelFiles(SharedState history)
		{
			var files = new List<string>();

			if (history.TryGetStateFromAll<string[]>("sentinel_files", out var s))
			{
				foreach (var set in s)
					files.AddRange(set);
			}

			return files;
		}

		public override IEnumerable<CheckupDependency> DeclareDependencies(IEnumerable<string> checkupIds)
			=> new [] {
				new CheckupDependency("dotnet"),
				new CheckupDependency("vswin", false),
				new CheckupDependency("vsmac", false)
			};

		public override bool ShouldExamine(SharedState history)
			=> GetAllSentinelFiles(history)?.Any() ?? false;

		public override Task<DiagnosticResult> Examine(SharedState history)
		{
			var files = GetAllSentinelFiles(history);

			var missingFiles = new List<string>();

			foreach (var file in files.Distinct())
			{
				// Check if exists
				if (!File.Exists(file))
				{
					try { File.Create(file); }
					catch { }
				}

				if (!file.Contains("omnisharp"))
				{
					if (!File.Exists(file))
					{
						ReportStatus(file + " does not exist.", Status.Error);
						missingFiles.Add(file);
					}
					else
					{
						ReportStatus(file + " exists.", Status.Ok);
					}
				}
			}

			if (missingFiles.Any())
			{
				return Task.FromResult(
					new DiagnosticResult(Status.Error, this, new Suggestion("Create EnableWorkloadResolver.sentinel files.",
						missingFiles.Select(f => new CreateFileSolution(f)).ToArray())));
			}

			return Task.FromResult(DiagnosticResult.Ok(this));
		}
	}
}
