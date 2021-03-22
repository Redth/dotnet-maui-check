using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace MauiDoctor.AndroidSdk
{
	internal class AdbRunner
	{
		public AdbRunner(SdkTool sdkTool)
		{
			this.sdkTool = sdkTool;
		}

		SdkTool sdkTool;

		internal void AddSerial(string serial, ProcessArgumentBuilder builder)
		{
			if (!string.IsNullOrEmpty(serial))
			{
				builder.Append("-s");
				builder.Append(serial);
			}
		}

		internal ShellProcessRunner.ShellProcessResult RunAdb(DirectoryInfo androidSdkHome, ProcessArgumentBuilder builder)
			=> RunAdb(androidSdkHome, builder, System.Threading.CancellationToken.None);

		internal ShellProcessRunner.ShellProcessResult RunAdb(DirectoryInfo androidSdkHome, ProcessArgumentBuilder builder, System.Threading.CancellationToken cancelToken)
		{
			var adbToolPath = sdkTool.FindToolPath(androidSdkHome);
			if (adbToolPath == null || !File.Exists(adbToolPath.FullName))
				throw new FileNotFoundException("Could not find adb", adbToolPath?.FullName);

			var p = new ShellProcessRunner(new ShellProcessRunnerOptions(adbToolPath.FullName, builder.ToString(), cancelToken));

			return p.WaitForExit();
		}
	}
}