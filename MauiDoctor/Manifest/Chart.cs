using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

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
}
