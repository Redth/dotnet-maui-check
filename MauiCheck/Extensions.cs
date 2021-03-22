using System;
using NuGet.Versioning;

namespace DotNetCheck
{
	public static class Extensions
	{
		public static bool IsCompatible(this SemanticVersion v, SemanticVersion minimumVersion, SemanticVersion exactVersion = null)
		{
			if (exactVersion != null)
				return v.Equals(exactVersion);

			return v >= minimumVersion;
		}

		public static SemanticVersion ThisOrExact(this SemanticVersion minimumVersion, SemanticVersion exactVersion)
			=> exactVersion == null ? minimumVersion : exactVersion;

		public static NuGetVersion ThisOrExact(this NuGetVersion minimumVersion, NuGetVersion exactVersion)
			=> exactVersion == null ? minimumVersion : exactVersion;
	}
}
