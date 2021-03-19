using MauiDoctor.Doctoring;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MauiDoctor.Remedies
{
	public class CreateFileRemedy : Remedy
	{
		public CreateFileRemedy(string filename)
			: base()
		{
			Filename = filename;
		}

		public readonly string Filename;

		public override bool RequiresAdmin(Platform platform)
			=> true;

		public override Task Cure(CancellationToken cancellationToken)
		{
			if (!File.Exists(Filename))
			{
				File.Create(Filename);
				ReportStatus("Created: " + Filename);
			}

			return base.Cure(cancellationToken);
		}
	}
}
