using System;
using System.Threading;
using System.Threading.Tasks;

namespace MauiDoctor.Doctoring
{
	public class ActionRemedy : Remedy
	{
		public ActionRemedy(Func<Remedy, CancellationToken, Task> curer)
		{
			Curer = curer;
		}

		public Func<Remedy, CancellationToken, Task> Curer { get; private set; }

		public override async Task Cure(CancellationToken cancellationToken)
		{
			await base.Cure(cancellationToken);

			await Curer?.Invoke(this, cancellationToken);
		}
	}
}
