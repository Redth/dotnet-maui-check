using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MauiDoctor.Doctoring
{
	public abstract class Checkup
	{
		public abstract string Id { get; }

		public virtual string[] Dependencies { get; }  = Array.Empty<string>();

		public abstract string Title { get; }
		public virtual string Description { get; } = string.Empty;

		public abstract Task<Diagonosis> Examine(PatientHistory history);

		public virtual bool IsPlatformSupported(Platform platform)
			=> true;

		protected void ReportStatus(string message, Status? status, int progress = -1)
			=> OnStatusUpdated?.Invoke(this, new CheckupStatusEventArgs(this, message, status, progress));

		public event EventHandler<CheckupStatusEventArgs> OnStatusUpdated;
	}
}
