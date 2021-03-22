using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.InteropServices;

namespace MauiDoctor.AndroidSdk
{
	public partial class AvdManager : SdkTool
	{
		public AvdManager()
			: this((DirectoryInfo)null)
		{ }

		public AvdManager(DirectoryInfo androidSdkHome)
			: base(androidSdkHome)
		{
			AndroidSdkHome = androidSdkHome;
		}

		public AvdManager(string androidSdkHome)
			: this(new DirectoryInfo(androidSdkHome))
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

			likelyPathSegments.Insert(0, new[] { "tools", "bin" });

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

			run(interactive, args.ToArray());
		}

		public void Delete(string name)
		{
			run("delete", "avd", "-n", name);
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

			run(args.ToArray());
		}

		static Regex rxListTargets = new Regex(@"id:\s+(?<id>[^\n]+)\s+Name:\s+(?<name>[^\n]+)\s+Type\s?:\s+(?<type>[^\n]+)\s+API level\s?:\s+(?<api>[^\n]+)\s+Revision\s?:\s+(?<revision>[^\n]+)", RegexOptions.Multiline | RegexOptions.Compiled);

		public IEnumerable<AvdTarget> ListTargets()
		{
			var r = new List<AvdTarget>();

			var lines = run("list", "target");

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

			var lines = run("list", "avd");

			var str = string.Join("\n", lines);

			var matches = rxListAvds.Matches(str);
			if (matches != null && matches.Count > 0)
			{
				foreach (Match m in matches)
				{
					var a = new Avd
					{
						Name = m.Groups?["name"]?.Value,
						Device = m.Groups?["device"]?.Value,
						Path = m.Groups?["path"]?.Value,
						Target = m.Groups?["target"]?.Value,
						BasedOn = m.Groups?["basedon"]?.Value
					};

					if (!string.IsNullOrWhiteSpace(a.Name))
					{
						a.ParseConfig();
						r.Add(a);
					}
				}
			}

			return r;
		}

		static Regex rxListDevices = new Regex(@"id:\s+(?<id>[^\n]+)\s+Name:\s+(?<name>[^\n]+)\s+OEM\s?:\s+(?<oem>[^\n]+)", RegexOptions.Singleline | RegexOptions.Compiled);
		static Regex rxDeviceId = new Regex(@"(?<num>\d+)\s+or\s+""(?<name>(.*?))""");

		public IEnumerable<AvdDevice> ListDevices()
		{
			var r = new List<AvdDevice>();

			var lines = run("list", "device");

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


		IEnumerable<string> run(params string[] args)
			=> run(false, args);

		IEnumerable<string> run(bool interactive, params string[] args)
		{
			var adbManager = FindToolPath(AndroidSdkHome);
			if (adbManager == null || !File.Exists(adbManager.FullName))
				throw new FileNotFoundException("Could not find avdmanager", adbManager?.FullName);

			var builder = new ProcessArgumentBuilder();

			foreach (var arg in args)
				builder.Append(arg);

			var p = new ShellProcessRunner(new ShellProcessRunnerOptions(adbManager.FullName, builder.ToString())
			{
				RedirectOutput = !interactive,
			});

			var r = p.WaitForExit();

			return r.StandardOutput;
		}
	}
}
