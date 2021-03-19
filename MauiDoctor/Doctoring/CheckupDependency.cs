using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MauiDoctor.Doctoring
{
	public class CheckupDependency
	{
		public CheckupDependency(string checkupId, bool isRequired = true)
		{
			CheckupId = checkupId;
			IsRequired = isRequired;
		}

		public string CheckupId { get; set; }
		public bool IsRequired { get; set; }
	}
}
