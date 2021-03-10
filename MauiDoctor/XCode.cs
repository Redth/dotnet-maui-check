using System;
using System.Threading.Tasks;

namespace MauiDoctor
{
	public class XCode
	{
		public XCode()
		{
		}

		public Task<XCodeInfo> GetInfo()
		{
			//Xcode 12.4
			//Build version 12D4e
			var r = ShellProcessRunner.Run("xcodebuild", "-version");

			var info = new XCodeInfo();

			foreach (var line in r.StandardOutput)
			{
				if (line.StartsWith("Xcode"))
				{
					var vstr = line.Substring(5).Trim();
					if (Version.TryParse(vstr, out var v))
						info.Version = v;
				}
				else if (line.StartsWith("Build version"))
				{
					info.Build = line.Substring(13);
				}
			}

			return Task.FromResult(info);
		}
	}

	public struct XCodeInfo
	{
		public Version Version;
		public string Build;
	}
}
