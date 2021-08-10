using DotNetCheck.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DotNetCheck.Solutions
{
	public class ActionSolution : Solution
	{
		public ActionSolution(Func<Solution, CancellationToken, Task> curer)
		{
			Curer = curer;
		}

		public Func<Solution, CancellationToken, Task> Curer { get; private set; }

		public override async Task Implement(SharedState sharedState, CancellationToken cancellationToken)
		{
			await base.Implement(sharedState, cancellationToken);

			await Curer?.Invoke(this, cancellationToken);
		}
	}
}
