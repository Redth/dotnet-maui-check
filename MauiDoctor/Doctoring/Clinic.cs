using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MauiDoctor.Doctoring
{
	public class Clinic
	{
		public Clinic()
		{
		}

		readonly List<Checkup> checkups = new List<Checkup>();

		public void OfferService(Checkup checkup)
			=> checkups.Add(checkup);

		public IEnumerable<Checkup> ScheduleCheckups()
		{
			var filtered = checkups.Where(c => c.IsPlatformSupported(Util.Platform));

			var sorted = TopologicalSort<Checkup>(filtered, c =>
				checkups.Where(dc => c.Dependencies.Contains(dc.Id)));

			return sorted;
		}

		static IEnumerable<T> TopologicalSort<T>(IEnumerable<T> nodes,
												Func<T, IEnumerable<T>> connected)
		{
			var elems = nodes.ToDictionary(node => node,
										   node => new HashSet<T>(connected(node)));
			while (elems.Count > 0)
			{
				var elem = elems.FirstOrDefault(x => x.Value.Count == 0);
				if (elem.Key == null)
				{
					throw new ArgumentException("Cyclic connections are not allowed");
				}
				elems.Remove(elem.Key);
				foreach (var selem in elems)
				{
					selem.Value.Remove(elem.Key);
				}
				yield return elem.Key;
			}
		}
	}
}
