using System;
using System.Threading;
using System.Threading.Tasks;

namespace MauiDoctor.Doctoring
{
	public class ActionRemedy : Remedy
	{
		public ActionRemedy(Func<CancellationToken, Task> curer)
		{
			Curer = curer;
		}

		public Func<CancellationToken, Task> Curer { get; private set; }

		public override async Task Cure(CancellationToken cancellationToken)
		{
			await base.Cure(cancellationToken);

			await Curer?.Invoke(cancellationToken);
		}
	}
}
