using DotNetCheck.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DotNetCheck.Solutions
{
	public class CreateFileSolution : Solution
	{
		public CreateFileSolution(string filename)
			: base()
		{
			Filename = filename;
		}

		public readonly string Filename;

		public override async Task Implement(CancellationToken cancellationToken)
		{
			if (!File.Exists(Filename))
			{
				await Util.WrapWithShellCopy(Filename, true, f =>
				{
					File.Create(f);
					return Task.FromResult(true);
				});

				ReportStatus("Created: " + Filename);
			}

			await base.Implement(cancellationToken);
		}
	}
}
