using DotNetCheck.Manifest;
using DotNetCheck.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace DotNetCheck.Models
{
	public abstract class CheckupContributor
	{
		public abstract IEnumerable<Checkup> Contribute(Manifest.Manifest manifest, SharedState sharedState);
	}
}
