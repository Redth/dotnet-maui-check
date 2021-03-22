
using System.Diagnostics;

namespace DotNetCheck.AndroidSdk
{
	/// <summary>
	/// Android SDK Package Information.
	/// </summary>
	[DebuggerDisplay("{Path,nq};{Version,nq}")]
	public class SdkPackage
	{
		/// <summary>
		/// Gets or sets the SDK Manager path.
		/// </summary>
		/// <value>The path.</value>
		public string Path { get; set; }
		/// <summary>
		/// Gets or sets the package version.
		/// </summary>
		/// <value>The version.</value>
		public string Version { get; set; }
		/// <summary>
		/// Gets or sets the package description.
		/// </summary>
		/// <value>The description.</value>
		public string Description { get; set; }
	}
}
