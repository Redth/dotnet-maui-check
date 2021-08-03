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
	public class NuGetFeedVariableMapper : VariableMapper
	{
		[JsonProperty("packageSources")]
		public string[] PackageSources { get; set; }

		[JsonProperty("mappings")]
		public List<NuGetPackageVersionVariableMapping> Mappings { get; set; } = new List<NuGetPackageVersionVariableMapping>();

		public override async Task Map()
		{
			var nugetCache = NullSourceCacheContext.Instance;
			var nugetLogger = NullLogger.Instance;


			foreach (var mapping in Mappings)
			{
				var currentFoundVersion = new NuGetVersion(0, 0, 0);

				var floatRange = FloatRange.Parse(mapping.PackageVersion);

				foreach (var pkgSrc in PackageSources)
				{
					var nugetSource = Repository.Factory.GetCoreV3(pkgSrc);

					var byIdRes = await nugetSource.GetResourceAsync<FindPackageByIdResource>();

					// Cause a retry if this is null
					if (byIdRes == null)
						throw new InvalidDataException();

					var versions = await byIdRes.GetAllVersionsAsync(mapping.PackageId, nugetCache, nugetLogger, CancellationToken.None);

					if (versions != null)
					{
						foreach (var v in versions)
						{
							if (floatRange.FloatBehavior != NuGetVersionFloatBehavior.None)
							{
								if (floatRange.Satisfies(v) && v > currentFoundVersion)
									currentFoundVersion = v;
							}
							else if (floatRange.HasMinVersion && floatRange.MinVersion == v && v >= currentFoundVersion)
							{
								currentFoundVersion = v;
							}
						}
					}
				}

				if (currentFoundVersion > new NuGetVersion(0,0,0))
					this.Variables[mapping.Name] = currentFoundVersion.ToString();
			}
		}
	}

	public class NuGetPackageVersionVariableMapping
	{
		[JsonProperty("name")]
		public string Name { get; set; }

		[JsonProperty("packageId")]
		public string PackageId { get; set; }

		[JsonProperty("packageVersion")]
		public string PackageVersion { get; set; }
	}
}
