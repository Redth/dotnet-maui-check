using DotNetCheck.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DotNetCheck.Solutions
{
	public class ActionSolution : Solution
	{
		public ActionSolution(Func<CancellationToken, Task> curer)
		{
			Curer = curer;
		}

		public Func<CancellationToken, Task> Curer { get; private set; }

		public override async Task Implement(SharedState sharedState, CancellationToken cancellationToken)
		{
			await base.Implement(sharedState, cancellationToken);

			await Curer?.Invoke(cancellationToken);
		}
	}
}
