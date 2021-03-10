using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace MauiDoctor.AndroidSdk
{
	public abstract class SdkTool
	{
		public SdkTool()
		{ }

		public SdkTool(string androidSdkHome)
			: this(new DirectoryInfo(androidSdkHome))
		{ }

		public SdkTool(DirectoryInfo androidSdkHome)
		{
			AndroidSdkHome = androidSdkHome;
		}

		internal abstract string SdkPackageId { get; }

		public DirectoryInfo AndroidSdkHome { get; internal set; }

		public abstract FileInfo FindToolPath(DirectoryInfo androidSdkHome = null);

		internal FileInfo FindTool(DirectoryInfo androidHome, string toolName, string windowsExtension, params string[] pathSegments)
		{
			var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

			var ext = isWindows ? windowsExtension : string.Empty;
			var home = AndroidSdkManager.FindHome(androidHome)?.FirstOrDefault();

			if (home?.Exists ?? false)
			{
				var allSegments = new List<string>();
				allSegments.Add(home.FullName);
				allSegments.AddRange(pathSegments);
				allSegments.Add(toolName + ext);

				var tool = Path.Combine(allSegments.ToArray());

				if (File.Exists(tool))
					return new FileInfo(tool);
			}

			return null;
		}


		public void Acquire()
		{
			if (FindToolPath(AndroidSdkHome)?.Exists ?? false)
				return;

			var sdkManager = new SdkManager(AndroidSdkHome);

			sdkManager.Acquire(SdkPackageId);
		}
	}
}
