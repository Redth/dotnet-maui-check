using System.Collections.Generic;
using System.Text;

namespace DotNetCheck.AndroidSdk
{
	public partial class SdkManager
	{
		/// <summary>
		/// Encapsulates results from Android SDK Manager's listing
		/// </summary>
		public class SdkManagerList
		{
			/// <summary>
			/// Gets or sets the available packages to install.
			/// </summary>
			/// <value>The available packages.</value>
			public List<SdkPackage> AvailablePackages { get; set; } = new List<SdkPackage>();

			/// <summary>
			/// Gets or sets the already installed packages.
			/// </summary>
			/// <value>The installed packages.</value>
			public List<InstalledSdkPackage> InstalledPackages { get; set; } = new List<InstalledSdkPackage>();

			public override string ToString()
			{
				var s = new StringBuilder();

				var writeHeaders = AvailablePackages.Count > 0 && InstalledPackages.Count > 0;

				if (AvailablePackages.Count > 0)
				{
					if (writeHeaders)
						s.AppendLine("Available:");

					foreach (var a in AvailablePackages)
						s.AppendLine($"{a.Path} | {a.Version} | {a.Description}");
				}

				if (InstalledPackages.Count > 0)
				{
					if (writeHeaders)
						s.AppendLine("Installed:");

					foreach (var a in InstalledPackages)
						s.AppendLine($"{a.Path} | {a.Version} | {a.Description}");
				}

				return s.ToString();
			}
		}
	}
}
