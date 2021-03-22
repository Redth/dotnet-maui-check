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

		public override string Title => $".NET Core SDK - EnableWorkloadResolver.sentinel";

		public override IEnumerable<CheckupDependency> Dependencies
			=> new List<CheckupDependency> {
				new CheckupDependency("dotnet"),
				new CheckupDependency("visualstudio", false)
			};

		public override Task<DiagnosticResult> Examine(SharedState history)
		{
			var files = new List<string>();

			var contributingCheckupIds = new[] { "dotnet", "visualstudio" };

			foreach (var checkupId in contributingCheckupIds)
			{
				if (history.TryGetState<string[]>(checkupId, "sentinel_files", out var dotnetSentinelFiles) && (dotnetSentinelFiles?.Any() ?? false))
					files.AddRange(dotnetSentinelFiles);
			}

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
