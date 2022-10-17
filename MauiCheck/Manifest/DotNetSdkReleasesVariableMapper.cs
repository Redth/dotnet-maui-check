using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
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
	public class DotNetSdkReleasesVariableMapper : VariableMapper
	{
		[JsonProperty("releasesIndexJsonUrl")]
		public string ReleasesIndexJsonUrl { get; set; } = "https://dotnetcli.blob.core.windows.net/dotnet/release-metadata/releases-index.json";

		[JsonProperty("channelVersion")]
		public string ChannelVersion { get; set; }

		[JsonProperty("name")]
		public string Name { get; set; }

		public override async Task Map()
		{
			var http = new HttpClient();

			var json = await http.GetStringAsync(ReleasesIndexJsonUrl);

			var releases = Newtonsoft.Json.JsonConvert.DeserializeObject<DotNetReleasesVariableMapperReleases>(json);

			
			foreach (var release in releases.Releases)
			{
				if (release.ChannelVersion.Equals(ChannelVersion, StringComparison.OrdinalIgnoreCase))
				{
					this.Variables[Name] = release.LatestSdk;
				}
			}
		}
	}

	public class DotNetSdkReleasesVariableMapping
	{
		[JsonProperty("name")]
		public string Name { get; set; }

		[JsonProperty("channelVersion")]
		public string ChannelVersion { get; set; }
	}

	partial class DotNetReleasesVariableMapperReleases
	{
		[JsonProperty("releases-index")]
		public DotNetReleasesVariableMapperReleasesRelease[] Releases { get; set; }
	}

	public partial class DotNetReleasesVariableMapperReleasesRelease
	{
		[JsonProperty("channel-version")]
		public string ChannelVersion { get; set; }

		[JsonProperty("latest-release")]
		public string LatestRelease { get; set; }

		[JsonProperty("latest-release-date")]
		public DateTimeOffset LatestReleaseDate { get; set; }

		[JsonProperty("security")]
		public bool Security { get; set; }

		[JsonProperty("latest-runtime")]
		public string LatestRuntime { get; set; }

		[JsonProperty("latest-sdk")]
		public string LatestSdk { get; set; }

		[JsonProperty("product")]
		public Product Product { get; set; }

		[JsonProperty("support-phase")]
		public string SupportPhase { get; set; }

		[JsonProperty("eol-date")]
		public DateTimeOffset? EolDate { get; set; }

		[JsonProperty("releases.json")]
		public Uri ReleasesJson { get; set; }
	}
}
