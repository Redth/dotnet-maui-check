using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DotNetCheck.Models
{
	public abstract class Checkup
	{
		public Checkup()
		{ }

		public abstract string Id { get; }

		public virtual IEnumerable<CheckupDependency> DeclareDependencies(IEnumerable<string> checkupIds)
			=> Enumerable.Empty<CheckupDependency>();

		public abstract string Title { get; }
		public virtual string Description { get; } = string.Empty;

		public Manifest.Manifest Manifest { get; internal set; }

		public virtual bool ShouldExamine(SharedState history)
		{
			return true;
		}

		public abstract Task<DiagnosticResult> Examine(SharedState history);

		public virtual bool IsPlatformSupported(Platform platform)
			=> true;

		protected void ReportStatus(string message, Status? status, int progress = -1)
			=> OnStatusUpdated?.Invoke(this, new CheckupStatusEventArgs(this, message, status, progress));

		public event EventHandler<CheckupStatusEventArgs> OnStatusUpdated;
	}
}
