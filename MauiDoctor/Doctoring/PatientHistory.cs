using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MauiDoctor.Doctoring
{
	public class PatientHistory
	{
		Dictionary<string, Dictionary<string, object>> charts = new Dictionary<string, Dictionary<string, object>>();

		public void AddNotes(Checkup checkup, string key, object value)
		{
			var checkupId = checkup.Id;

			if (!charts.ContainsKey(checkupId))
				charts.Add(checkupId, new Dictionary<string, object>());

			charts[checkupId][key] = value;
		}

		public bool TryGetNotes<T>(string checkupId, string key, out T notes) where T : class
		{
			notes = default;

			if (charts.TryGetValue(checkupId, out var checkupNotes) && checkupNotes.TryGetValue(key, out var value))
				notes = value as T;

			return notes != default;
		}
	}
}
