using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.InteropServices;
using IniParser;
using System.Diagnostics;

namespace DotNetCheck.AndroidSdk
{
	public partial class AvdManager : SdkTool
	{
		public AvdManager(string java)
			: this(new FileInfo(java), (DirectoryInfo)null)
		{ }

		public AvdManager(FileInfo java)
			: this(java, (DirectoryInfo)null)
		{ }

		public AvdManager(FileInfo java, DirectoryInfo androidSdkHome)
			: base(java, androidSdkHome)
		{
			AndroidSdkHome = androidSdkHome;
			Java = java;
		}

		public AvdManager(string java, string androidSdkHome)
			: this(new FileInfo(java), new DirectoryInfo(androidSdkHome))
		{ }

		internal override string SdkPackageId => "emulator";


		//public override FileInfo FindToolPath(DirectoryInfo androidSdkHome)
		//	=> FindTool(androidSdkHome, toolName: "avdmanager", windowsExtension: ".bat", "tools", "bin");

		public override FileInfo FindToolPath(DirectoryInfo androidSdkHome)
		{
			var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
			var ext = isWindows ? ".bat" : string.Empty;

			var likelyPathSegments = new List<string[]>();

			var cmdlineToolsPath = new DirectoryInfo(Path.Combine(androidSdkHome.FullName, "cmdline-tools"));

			if (cmdlineToolsPath.Exists)
			{
				foreach (var dir in cmdlineToolsPath.GetDirectories())
				{
					var toolPath = new FileInfo(Path.Combine(dir.FullName, "bin", "avdmanager" + ext));
					if (toolPath.Exists)
						likelyPathSegments.Insert(0, new[] { "cmdline-tools", dir.Name, "bin" });
				}
			}

			likelyPathSegments.Add(new[] { "tools", "bin" });

			foreach (var pathSeg in likelyPathSegments)
			{
				var tool = FindTool(androidSdkHome, toolName: "avdmanager", windowsExtension: ".bat", pathSeg);
				if (tool != null)
					return tool;
			}

			return null;
		}


		public void Create(string name, string sdkId, string device = null, string path = null, string tag = null, bool force = false, bool interactive = false)
		{
			var args = new List<string> {
				"create", "avd", "--name", name, "--package", $"\"{sdkId}\""
			};

			if (!string.IsNullOrEmpty(device))
			{
				args.Add("--device");
				args.Add($"\"{device}\"");
			}

			if (!string.IsNullOrEmpty(path))
			{
				args.Add("-c");
				args.Add($"\"{path}\"");
			}

			if (force)
				args.Add("--force");

			if (!string.IsNullOrEmpty(path))
			{
				args.Add("-p");
				args.Add($"\"{path}\"");
			}

			AvdManagerRun(args.ToArray());
		}

		public void Delete(string name)
		{
			AvdManagerRun("delete", "avd", "-n", name);
		}

		public void Move(string name, string path = null, string newName = null)
		{
			var args = new List<string> {
				"move", "avd", "-n", name
			};

			if (!string.IsNullOrEmpty(path))
			{
				args.Add("-p");
				args.Add(path);
			}

			if (!string.IsNullOrEmpty(newName))
			{
				args.Add("-r");
				args.Add(newName);
			}

			AvdManagerRun(args.ToArray());
		}

		static Regex rxListTargets = new Regex(@"id:\s+(?<id>[^\n]+)\s+Name:\s+(?<name>[^\n]+)\s+Type\s?:\s+(?<type>[^\n]+)\s+API level\s?:\s+(?<api>[^\n]+)\s+Revision\s?:\s+(?<revision>[^\n]+)", RegexOptions.Multiline | RegexOptions.Compiled);

		public IEnumerable<AvdTarget> ListTargets()
		{
			var r = new List<AvdTarget>();

			var lines = AvdManagerRun("list", "target");

			var str = string.Join("\n", lines);

			var matches = rxListTargets.Matches(str);
			if (matches != null && matches.Count > 0)
			{
				foreach (Match m in matches)
				{
					var a = new AvdTarget
					{
						Name = m.Groups?["name"]?.Value,
						Id = m.Groups?["id"]?.Value,
						Type = m.Groups?["type"]?.Value
					};

					if (int.TryParse(m.Groups?["api"]?.Value, out var api))
						a.ApiLevel = api;
					if (int.TryParse(m.Groups?["revision"]?.Value, out var rev))
						a.Revision = rev;

					if (!string.IsNullOrWhiteSpace(a.Id) && a.ApiLevel > 0)
						r.Add(a);
				}
			}

			return r;
		}

		static Regex rxListAvds = new Regex(@"\s+Name:\s+(?<name>[^\n]+)(\s+Device:\s+(?<device>[^\n]+))?\s+Path:\s+(?<path>[^\n]+)\s+Target:\s+(?<target>[^\n]+)\s+Based on:\s+(?<basedon>[^\n]+)", RegexOptions.Compiled | RegexOptions.Multiline);
		public IEnumerable<Avd> ListAvds()
		{
			var r = new List<Avd>();

			var lines = AvdManagerRun("list", "avd");

			var str = string.Join("\n", lines);

			var matches = rxListAvds.Matches(str);
			if (matches != null && matches.Count > 0)
			{
				foreach (Match m in matches)
				{
					var path = m.Groups?["path"]?.Value;

					if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
					{
						var avd = Avd.From(path);

						var parsedName = m.Groups?["name"]?.Value;
						if (string.IsNullOrEmpty(avd.Name) && !string.IsNullOrEmpty(parsedName))
							avd.Name = parsedName;

						var parsedDevice = m.Groups?["device"]?.Value;
						if (string.IsNullOrEmpty(avd.Device) && !string.IsNullOrEmpty(parsedDevice))
							avd.Device = parsedDevice;

						var parsedTarget = m.Groups?["target"]?.Value;
						if (string.IsNullOrEmpty(avd.Target) && !string.IsNullOrEmpty(parsedTarget))
							avd.Target = parsedTarget;

						var parsedBasedOn = m.Groups?["basedon"]?.Value;
						if (string.IsNullOrEmpty(avd.BasedOn) && !string.IsNullOrEmpty(parsedBasedOn))
							avd.BasedOn = parsedBasedOn;

						r.Add(avd);
					}
				}
			}

			return r;
		}

		public static IEnumerable<Avd> ListAvdsFromFiles()
		{
			var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".android", "avd");

			if (Directory.Exists(folder))
			{
				var files = Directory.EnumerateFiles(folder, "*.ini", SearchOption.TopDirectoryOnly);

				foreach (var file in files)
				{
					Avd avd = default;

					try
					{
						var ini = Avd.ParseIni(file);

						if (ini.TryGetValue("path", out var avdPath))
						{
							if (Directory.Exists(avdPath))
							{
								avd = Avd.From(avdPath);
							}
						}
					}
					catch { }

					if (avd != null)
						yield return avd;
				}
			}
		}

		static Regex rxListDevices = new Regex(@"id:\s+(?<id>[^\n]+)\s+Name:\s+(?<name>[^\n]+)\s+OEM\s?:\s+(?<oem>[^\n]+)", RegexOptions.Singleline | RegexOptions.Compiled);
		static Regex rxDeviceId = new Regex(@"(?<num>\d+)\s+or\s+""(?<name>(.*?))""");

		public IEnumerable<AvdDevice> ListDevices()
		{
			var r = new List<AvdDevice>();

			var lines = AvdManagerRun("list", "device");

			var str = string.Join("\n", lines);

			var matches = rxListDevices.Matches(str);
			if (matches != null && matches.Count > 0)
			{
				foreach (Match m in matches)
				{
					var id = m.Groups?["id"]?.Value;

					if (!string.IsNullOrEmpty(id))
					{
						var idMatch = rxDeviceId.Match(id);

						if (idMatch?.Success ?? false)
						{
							var a = new AvdDevice
							{
								Name = m.Groups?["name"]?.Value,
								Id = idMatch.Groups?["name"]?.Value,
								Oem = m.Groups?["oem"]?.Value
							};

							if (!string.IsNullOrWhiteSpace(a.Name))
								r.Add(a);
						}
					}
				}
			}

			return r;
		}


		IEnumerable<string> AvdManagerRun(params string[] args)
		{
			var adbManager = FindToolPath(AndroidSdkHome);
			var java = Java;

			var libPath = Path.GetFullPath(Path.Combine(adbManager.DirectoryName, "..", "lib"));
			var toolPath = Path.GetFullPath(Path.Combine(adbManager.DirectoryName, ".."));

			var cpSeparator = Util.IsWindows ? ';' : ':';

			// Get all the .jars in the tools\lib folder to use as classpath
			//var classPath = "avdmanager-classpath.jar";
			var classPath = string.Join(cpSeparator, Directory.GetFiles(libPath, "*.jar").Select(f => new FileInfo(f).Name));

			var proc = new Process();
			// This is the package and class that contains the main() for avdmanager
			proc.StartInfo.Arguments = "com.android.sdklib.tool.AvdManagerCli " + string.Join(' ', args);
			// This needs to be set to the working dir / classpath dir as the library looks for this system property at runtime
			proc.StartInfo.Environment["JAVA_TOOL_OPTIONS"] = $"-Dcom.android.sdkmanager.toolsdir=\"{toolPath}\"";
			// Set the classpath to all the .jar files we found in the lib folder
			proc.StartInfo.Environment["CLASSPATH"] = classPath;

			// Java.exe
			proc.StartInfo.FileName = Java.FullName;
			// lib folder is our working dir
			proc.StartInfo.WorkingDirectory = libPath;

			proc.StartInfo.UseShellExecute = false;
			proc.StartInfo.RedirectStandardOutput = true;
			proc.StartInfo.RedirectStandardError = true;

			var output = new List<string>();

			proc.OutputDataReceived += (s, e) =>
			{
				if (!string.IsNullOrEmpty(e.Data))
					output.Add(e.Data);
			};
			proc.ErrorDataReceived += (s, e) =>
			{
				if (!string.IsNullOrEmpty(e.Data))
					output.Add(e.Data);
			};

			proc.Start();
			proc.BeginOutputReadLine();
			proc.BeginErrorReadLine();
			proc.WaitForExit();

			return output;
		}
	}
}
