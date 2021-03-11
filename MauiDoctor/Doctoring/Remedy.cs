using System;
using System.Linq;
using System.Threading.Tasks;

namespace MauiDoctor.Doctoring
{
	public abstract class Remedy
	{
		public Remedy()
		{ }

		public Remedy(params (Platform platform, bool requiresAdmin)[] adminRequirements)
		{
			AdminRequirements = adminRequirements;
		}

		public (Platform platform, bool requiresAdmin)[] AdminRequirements { get; set; }

		public virtual bool RequiresAdmin(Platform platform)
		{
			if (AdminRequirements?.Any() ?? false)
			{
				var adminReq = AdminRequirements.FirstOrDefault(ar => ar.platform == platform);
				if (adminReq != default)
					return adminReq.requiresAdmin;
			}

			return false;
		}

		public bool HasPrivilegesToRun(bool isAdmin, Platform platform)
			=> !RequiresAdmin(platform) || isAdmin;

		public virtual Task Cure(System.Threading.CancellationToken cancellationToken)
			=> Task.CompletedTask;

		public void ReportStatus(string message, int progress = -1)
			=> OnStatusUpdated?.Invoke(this, new CureStatusEventArgs(this, message, progress));

		public event EventHandler<CureStatusEventArgs> OnStatusUpdated;
	}
}
