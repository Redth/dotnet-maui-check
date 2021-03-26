using System;
using System.Linq;
using System.Threading.Tasks;

namespace DotNetCheck.Models
{
	public abstract class Solution
	{
		public Solution()
		{ }

		public virtual Task Implement(System.Threading.CancellationToken cancellationToken)
			=> Task.CompletedTask;

		public void ReportStatus(string message)
			=> OnStatusUpdated?.Invoke(this, new RemedyStatusEventArgs(this, message));

		public event EventHandler<RemedyStatusEventArgs> OnStatusUpdated;
	}
}
