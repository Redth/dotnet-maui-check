using System;
using System.Collections.Generic;

using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MauiDoctor.Manifest
{
	public partial class Chart
	{
		public static Task<Chart> FromFileOrUrl(string fileOrUrl)
		{
			if (fileOrUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
				return FromUrl(fileOrUrl);

			return FromFile(fileOrUrl);
		}

		public static async Task<Chart> FromFile(string filename)
		{
			var json = await System.IO.File.ReadAllTextAsync(filename);
			return FromJson(json);
		}

		public static async Task<Chart> FromUrl(string url)
		{
			var http = new HttpClient();
			var json = await http.GetStringAsync(url);

			return FromJson(json);
		}

		public static Chart FromJson(string json) => JsonConvert.DeserializeObject<Chart>(json);

		[JsonProperty("doctor")]
		public Doctor Doctor { get; set; }
	}

	public partial class Doctor
	{
		[JsonProperty("xcode")]
		public MinExactVersion XCode { get; set; }

		[JsonProperty("vswin")]
		public MinExactVersion VSWin { get; set; }

		[JsonProperty("vsmac")]
		public MinExactVersion VSMac { get; set; }

		[JsonProperty("android")]
		public Android Android { get; set; }

		[JsonProperty("dotnet")]
		public DotNet DotNet { get; set; }
	}

	public partial class Android
	{
		[JsonProperty("packages")]
		public List<AndroidPackage> Packages { get; set; }
	}

	public partial class AndroidPackage
	{
		[JsonProperty("path")]
		public string Path { get; set; }

		[JsonProperty("version")]
		public string Version { get; set; }
	}

	public partial class DotNetSdk
	{
		[JsonProperty("urls")]
		public Urls Urls { get; set; }

		[JsonProperty("version")]
		public string Version { get; set; }

		[JsonProperty("packs")]
		public List<DotNetPack> Packs { get; set; }
	}

	public partial class DotNet
	{
		[JsonProperty("sdks")]
		public List<DotNetSdk> Sdks { get; set; }
	}

	public partial class Urls
	{
		[JsonProperty("win", NullValueHandling = NullValueHandling.Ignore)]
		public Uri Win { get; set; }

		[JsonProperty("osx", NullValueHandling = NullValueHandling.Ignore)]
		public Uri Osx { get; set; }

		[JsonProperty("linux", NullValueHandling = NullValueHandling.Ignore)]
		public Uri Linux { get; set; }

		public Uri For(Platform platform)
			=> platform switch
			{
				Platform.OSX => Osx,
				Platform.Windows => Win,
				Platform.Linux => Linux,
				_ => Win
			};
	}

	public partial class DotNetPack
	{
		[JsonProperty("id")]
		public string Id { get; set; }

		[JsonProperty("version")]
		public string Version { get; set; }

		[JsonProperty("workload")]
		public string Workload { get; set; }

		[JsonProperty("urls")]
		public Urls Urls { get; set; }

		public bool SupportedFor(Platform platform)
			=> Urls?.For(platform) != null;
	}

	public partial class MinExactVersion
	{
		[JsonProperty("minimumVersion")]
		public string MinimumVersion { get; set; }

		[JsonProperty("exactVersion")]
		public string ExactVersion { get; set; }
	}
}
