using System;
using System.Linq;
using System.Threading.Tasks;

namespace DotNetCheck.Models
{
	public abstract class Solution
	{
		public Solution()
		{ }

		public Solution(params (Platform platform, bool requiresAdmin)[] adminRequirements)
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

		public virtual Task Implement(System.Threading.CancellationToken cancellationToken)
			=> Task.CompletedTask;

		public void ReportStatus(string message)
			=> OnStatusUpdated?.Invoke(this, new RemedyStatusEventArgs(this, message));

		public event EventHandler<RemedyStatusEventArgs> OnStatusUpdated;
	}
}
