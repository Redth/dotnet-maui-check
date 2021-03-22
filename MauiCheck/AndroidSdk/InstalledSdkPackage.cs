
namespace DotNetCheck.AndroidSdk
{
	public partial class SdkManager
	{
		/// <summary>
		/// Installed Android SDK Package information.
		/// </summary>
		public class InstalledSdkPackage : SdkPackage
		{
			/// <summary>
			/// Gets or sets the Installed SDK package location.
			/// </summary>
			/// <value>The location.</value>
			public string Location { get; set; }
		}
	}
}
