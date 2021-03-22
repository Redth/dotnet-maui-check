using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DotNetCheck.Models
{
	public abstract class Checkup
	{
		public abstract string Id { get; }

		public virtual IEnumerable<CheckupDependency> Dependencies { get; } = Enumerable.Empty<CheckupDependency>();

		public abstract string Title { get; }
		public virtual string Description { get; } = string.Empty;

		public abstract Task<DiagnosticResult> Examine(SharedState history);

		public virtual bool IsPlatformSupported(Platform platform)
			=> true;

		protected void ReportStatus(string message, Status? status, int progress = -1)
			=> OnStatusUpdated?.Invoke(this, new CheckupStatusEventArgs(this, message, status, progress));

		public event EventHandler<CheckupStatusEventArgs> OnStatusUpdated;
	}
}
