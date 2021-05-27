using System;
using DotNetCheck.Cli;
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

		public static NuGetVersion ParseVersion(string version, NuGetVersion defaultVersion = null)
		{
			if (!string.IsNullOrEmpty(version) && NuGetVersion.TryParse(version, out var nv))
				return nv;

			return defaultVersion;
		}

		public static ManifestChannel GetManifestChannel(this IManifestChannelSettings settings)
		{
			var channel = ManifestChannel.Default;
			if (settings.Preview)
				channel = ManifestChannel.Preview;
			if (settings.Main)
				channel = ManifestChannel.Main;

			return channel;
		}
	}
}
