
namespace MauiDoctor.AndroidSdk
{
	public partial class SdkManager
	{
		/// <summary>
		/// Available Android Sdk Update package information.
		/// </summary>
		public class AvailableSdkUpdate
		{
			/// <summary>
			/// Gets or sets the Android SDK Manager path.
			/// </summary>
			/// <value>The path.</value>
			public string Path { get; set; }
			/// <summary>
			/// Gets or sets the available version of the package.
			/// </summary>
			/// <value>The installed version.</value>
			public string Version { get; set; }
			/// <summary>
			/// Gets or sets the package description.
			/// </summary>
			/// <value>The description.</value>
			public string Description { get; set; }
		}
	}
}
