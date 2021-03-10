using System;
using System.Threading;
using System.Threading.Tasks;

namespace MauiDoctor.Doctoring
{
	public class Prescription
	{
		public Prescription(string name)
			: this(name, (string)null, null)
		{
		}

		public Prescription(string name, params Remedy[] remedies)
			: this(name, null, remedies)
		{
		}

		public Prescription(string name, string description)
			: this(name, description, null)
		{
		}

		public Prescription(string name, string description, params Remedy[] remedies)
		{
			Name = name;
			Description = description;
			Remedies = remedies;
		}

		public string Name { get; private set; }

		public string Description { get; private set; }

		public bool HasRemedy
			=> Remedies != null && Remedies.Length > 0;

		public virtual Remedy[] Remedies { get; set; }
	}

	public abstract class Remedy
	{
		public virtual Task Cure(System.Threading.CancellationToken cancellationToken)
			=> Task.CompletedTask;

		public void ReportStatus(string message, int progress = -1)
			=> OnStatusUpdated?.Invoke(this, new CureStatusEventArgs(this, message, progress));

		public event EventHandler<CureStatusEventArgs> OnStatusUpdated;
	}

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
