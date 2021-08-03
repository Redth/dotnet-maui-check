using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;
using Newtonsoft.Json;

namespace DotNetCheck.Manifest
{
	public class XmlVariableMapper : VariableMapper
	{
		[JsonProperty("sourceUri")]
		public string SourceUri { get; set; }

		[JsonProperty("mappings")]
		public List<XPathVariableMapping> Mappings { get; set; } = new List<XPathVariableMapping>();

		public override Task Map()
		{
			var xml = XDocument.Load(SourceUri);

			foreach (var mapping in Mappings)
			{
				object value = default;

				var v = xml.XPathEvaluate(mapping.XPath);

				if (v is IEnumerable vcol)
				{
					var venum = vcol.GetEnumerator();
					venum.MoveNext();
					value = venum.Current;
				}
				else
					value = v;

				this.Variables[mapping.Name] = value;
			}

			return Task.CompletedTask;
		}
	}
}
