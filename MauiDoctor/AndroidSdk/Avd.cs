namespace MauiDoctor.AndroidSdk
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

			public string SdkId { get; private set; }

			internal void ParseConfig()
			{
				try
				{
					var configFile = System.IO.Path.Combine(Path, "config.ini");

					if (!System.IO.File.Exists(configFile))
						return;

					foreach (var line in System.IO.File.ReadAllLines(configFile))
					{
						if (line.StartsWith("image.sysdir.1="))
						{
							SdkId = line.Substring(15).Trim('/', '\\').Replace('/', ';').Replace('\\', ';');
							break;
						}
					}
				}
				catch { }
			}
		}
	}
}
