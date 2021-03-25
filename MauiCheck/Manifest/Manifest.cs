using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace DotNetCheck.Manifest
{
	public partial class Manifest
	{
		public const string DefaultManifestUrl = "https://aka.ms/dotnet-maui-check-manifest";
		public const string DevManifestUrl = "https://aka.ms/dotnet-maui-check-manifest-dev";

		public static Task<Manifest> FromFileOrUrl(string fileOrUrl)
		{
			if (fileOrUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
				return FromUrl(fileOrUrl);

			return FromFile(fileOrUrl);
		}

		public static async Task<Manifest> FromFile(string filename)
		{
			var json = await System.IO.File.ReadAllTextAsync(filename);
			return FromJson(json);
		}

		public static async Task<Manifest> FromUrl(string url)
		{
			var http = new HttpClient();
			var json = await http.GetStringAsync(url);

			return FromJson(json);
		}

		public static Manifest FromJson(string json) => JsonConvert.DeserializeObject<Manifest>(json);

		[JsonProperty("check")]
		public Check Check { get; set; }
	}
}
