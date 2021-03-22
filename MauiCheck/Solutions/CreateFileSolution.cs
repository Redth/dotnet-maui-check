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

		public override bool RequiresAdmin(Platform platform)
			=> true;

		public override Task Implement(CancellationToken cancellationToken)
		{
			if (!File.Exists(Filename))
			{
				File.Create(Filename);
				ReportStatus("Created: " + Filename);
			}

			return base.Implement(cancellationToken);
		}
	}
}
