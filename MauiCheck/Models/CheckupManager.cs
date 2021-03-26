using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DotNetCheck.Models
{
	public static class CheckupManager
	{
		static List<Checkup> registeredCheckups = new List<Checkup>();
		static List<CheckupContributor> registeredCheckupContributors = new List<CheckupContributor>();

		public static void RegisterCheckups(params Checkup[] checkups)
			=> registeredCheckups.AddRange(checkups);

		public static void RegisterCheckupContributors(params CheckupContributor[] checkupContributors)
			=> registeredCheckupContributors.AddRange(checkupContributors);

		public static IEnumerable<Checkup> BuildCheckupGraph(Manifest.Manifest manifest, SharedState sharedState)
		{
			var checkups = new List<Checkup>();

			checkups.AddRange(registeredCheckups);

			foreach (var c in registeredCheckupContributors)
			{
				var contributed = c.Contribute(manifest, sharedState);
				if (contributed?.Any() ?? false)
					checkups.AddRange(contributed);
			}

			var filtered = checkups.Where(c => c.IsPlatformSupported(Util.Platform));
			var checkupIds = filtered.Select(c => c.Id);

			var sorted = TopologicalSort<Checkup>(filtered, c =>
				filtered.Where(dc => c.DeclareDependencies(checkupIds).Any(d => dc.IsPlatformSupported(Util.Platform)
									&& d.CheckupId.StartsWith(dc.Id, StringComparison.OrdinalIgnoreCase))));

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
