using MauiDoctor.Doctoring;
using MauiDoctor.Remedies;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MauiDoctor.Checkups
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

		public override Task<Diagonosis> Examine(PatientHistory history)
		{
			var files = new List<string>();

			var contributingCheckupIds = new[] { "dotnet", "visualstudio" };

			foreach (var checkupId in contributingCheckupIds)
			{
				if (history.TryGetNotes<string[]>(checkupId, "sentinel_files", out var dotnetSentinelFiles) && (dotnetSentinelFiles?.Any() ?? false))
					files.AddRange(dotnetSentinelFiles);
			}

			var missingFiles = new List<string>();

			foreach (var file in files.Distinct())
			{
				// Check if exists
				if (!File.Exists(file))
					missingFiles.Add(file);
			}

			if (missingFiles.Any())
			{
				return Task.FromResult(
					new Diagonosis(Status.Error, this, new Prescription("Create EnableWorkloadResolver.sentinel files.",
						missingFiles.Select(f => new CreateFileRemedy(f)).ToArray())));
			}

			return Task.FromResult(Diagonosis.Ok(this));
		}
	}
}
