using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NuGet.Common;
using NuGet.Packaging.Core;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using Xamarin.Installer.Common;

namespace DotNetCheck.Manifest
{
	public class NuGetFeedWorkloadManifestSdkBandVariableMapper : VariableMapper
	{
		[JsonProperty("packageSources")]
		public string[] PackageSources { get; set; }

		[JsonProperty("mappings")]
		public List<NuGetFeedWorkloadManifestSdkBandVariableMapping> Mappings { get; set; } = new ();

		public override async Task Map()
		{
			var nugetCache = NullSourceCacheContext.Instance;
			var nugetLogger = NullLogger.Instance;


			foreach (var mapping in Mappings)
			{
				var packageFloatRange = FloatRange.Parse(mapping.PackageVersion);
				var sdkBandFloatRange = FloatRange.Parse(mapping.SdkBand);

				NuGetVersion currentSdkBand = null;
				string currentPackageId = null;
				NuGetVersion currentVersion = null;

				foreach (var pkgSrc in PackageSources)
				{
					var nugetSource = Repository.Factory.GetCoreV3(pkgSrc);

					var searchRes = await nugetSource.GetResourceAsync<PackageSearchResourceV3>();

					var results = await searchRes.SearchAsync(mapping.PackageId, new SearchFilter(mapping.IncludePrerelease), 0, 100, nugetLogger, CancellationToken.None);

					foreach (var result in results)
					{
						var parts = result.Identity.Id.Split('-', 2);
						if (parts.Length == 2)
						{
							if (NuGetVersion.TryParse(parts[1], out var sdkBand))
							{
								if (sdkBand.IsBetterVersion(currentSdkBand, sdkBandFloatRange, mapping.IncludePrerelease))
								{
									currentSdkBand = sdkBand;
									currentPackageId = result.Identity.Id;

									var versions = await result.GetVersionsAsync();
									if (versions != null)
									{
										foreach (var v in versions)
										{
											if (v.Version.IsBetterVersion(currentVersion, packageFloatRange, mapping.IncludePrerelease))
												currentVersion = v.Version;
										}
									}
								}
							}
						}
					}

				}

				if (currentVersion is not null && currentVersion > new NuGetVersion(0, 0, 0))
				{
					this.Variables[mapping.PackageVersionVariableName] = currentVersion.ToString();
				}
				if (currentSdkBand is not null && currentSdkBand > new NuGetVersion(0,0,0))
				{
					this.Variables[mapping.SdkBandVariableName] = currentSdkBand.ToString();
				}
				if (!string.IsNullOrEmpty(currentPackageId))
				{
					this.Variables[mapping.PackageIdVariableName] = currentPackageId;
				}
			}
		}
	}

	public static class NuGetVersionExtensions
	{
		public static bool IsBetterVersion(this NuGetVersion version, NuGetVersion currentVersion, FloatRange floatRange, bool includePrerelease)
		{
			if (!includePrerelease && version.IsPrerelease)
				return false;

			if (floatRange.FloatBehavior != NuGetVersionFloatBehavior.None)
			{
				if (floatRange.Satisfies(version) && version > (currentVersion ?? new NuGetVersion(0,0,0)))
					return true;
			}
			else if (floatRange.HasMinVersion && floatRange.MinVersion <= version && version >= (currentVersion ?? new NuGetVersion(0,0,0)))
			{
				return true;
			}

			return false;
		}
	}

	public class NuGetFeedWorkloadManifestSdkBandVariableMapping
	{
		[JsonProperty("packageIdVariableName")]
		public string PackageIdVariableName { get; set; }

		[JsonProperty("packageVersionVariableName")]
		public string PackageVersionVariableName { get; set; }

		[JsonProperty("sdkBandVariableName")]
		public string SdkBandVariableName { get; set; }

		[JsonProperty("sdkBand")]
		public string SdkBand { get; set; } = "*";

		[JsonProperty("packageId")]
		public string PackageId { get; set; }

		[JsonProperty("packageVersion")]
		public string PackageVersion { get; set; } = "*";

		[JsonProperty("includePrerelease")]
		public bool IncludePrerelease { get; set; }
	}
}
