using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DotNetCheck.Manifest
{
	public partial class Check
	{
		[JsonProperty("toolVersion")]
		public string ToolVersion { get; set; }

		[JsonProperty("variables")]
		public Dictionary<string, object> Variables { get; set; } = new Dictionary<string, object>();

		[JsonProperty("variableMappers")]
		public List<VariableMapper> VariableMappers { get; set; } = new List<VariableMapper>();

		[JsonProperty("openjdk")]
		public OpenJdk OpenJdk { get; set; }

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

		[JsonProperty("filepermissions")]
		public List<FilePermissions> FilePermissions { get; set; }

		public void MapVariables()
		{
			if (VariableMappers?.Any() ?? false)
			{
				foreach (var mapper in VariableMappers)
				{
					mapper.Map();

					foreach (var v in mapper.Variables)
						Variables[v.Key] = v.Value;
				}
			}

			SubstituteVariables(this, Variables);
		}

		void SubstituteVariables(object instance, Dictionary<string, object> variables)
		{
			var props = instance.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

			if (!(props?.Any() ?? false))
				return;

			foreach (var prop in props)
			{
				if (!prop.CanWrite)
					continue;

				var ptype = prop.PropertyType;
				var pval = prop.GetValue(instance);

				if (pval is string strVal)
				{
					strVal = SubstituteVariableValue(strVal, variables);
					prop.SetValue(instance, strVal);
				}
				else if (pval is IList pinstList)
				{
					foreach (var item in pinstList)
						SubstituteVariables(item, variables);
				}
				else if (ptype.IsClass && ptype.Assembly == typeof(Check).Assembly)
				{
					SubstituteVariables(pval, variables);
				}
			}
		}

		static string SubstituteVariableValue(string str, IDictionary<string, object> variables)
		{
			if (!string.IsNullOrEmpty(str))
			{
				foreach (var variable in variables)
					str = str.Replace($"$({variable.Key})", variable.Value?.ToString() ?? string.Empty);
			}

			return str;
		}
	}
}
