using IniParser;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DotNetCheck.AndroidSdk
{
	public partial class AvdManager
	{
		/// <summary>
		/// AVD Info
		/// </summary>
		public class Avd
		{
			/// <summary>
			/// Gets or sets the name.
			/// </summary>
			/// <value>The name.</value>
			public string Name { get; set; }

			public string Device { get; set; }
			public string Path { get; set; }
			public string Target { get; set; }

			public string BasedOn { get; set; }

			public override string ToString()
			{
				return $"{Name} | {Device} | {Target} | {Path} | {BasedOn}";
			}

			public string SdkId { get; internal set; }

			public static Avd From(string avdPath)
			{
				try
				{
					var avdIniFile = System.IO.Path.Combine(avdPath, "..", System.IO.Path.GetFileNameWithoutExtension(avdPath) + ".ini");
					var configIniFile = System.IO.Path.Combine(avdPath, "config.ini");

					if (File.Exists(avdIniFile))
					{
						var avdIni = ParseIni(avdIniFile);
						
						if (File.Exists(configIniFile))
						{
							var configIni = ParseIni(configIniFile);

							var avd = new Avd();
							avd.Name = System.IO.Path.GetFileNameWithoutExtension(avdIniFile);
							avd.Path = avdPath;
							if (avdIni.TryGetValue("target", out var avdTarget))
								avd.Target = avdTarget;
							if (configIni.TryGetValue("hw.device.name", out var avdDevice))
								avd.Device = avdDevice;
							if (configIni.TryGetValue("image.sysdir.1", out var avdBasedOn))
								avd.SdkId = avdBasedOn?.Trim('\\', '/')?.Replace('\\', ';')?.Replace('/', ';');

							return avd;
						}
					}
				}
				catch (Exception ex)
				{
					throw ex;
				}

				return null;
			}

			internal static Dictionary<string, string> ParseIni(string filename)
			{
				var d = new Dictionary<string, string>();

				if (File.Exists(filename))
				{
					foreach (var line in File.ReadAllLines(filename))
					{
						var parts = line?.Split(new char[] { '=' }, 2);

						if (parts?.Any() ?? false)
							d[parts[0]] = parts[1];
					}
				}

				return d;
			}
		}
	}
}
