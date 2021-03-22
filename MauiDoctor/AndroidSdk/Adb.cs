using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace MauiDoctor.AndroidSdk
{
	public partial class Adb : SdkTool
	{
		public Adb()
			: this((DirectoryInfo)null)
		{ }

		public Adb(DirectoryInfo androidSdkHome)
			: base(androidSdkHome)
		{
			runner = new AdbRunner(this);
		}

		public Adb(string androidSdkHome)
			: this(new DirectoryInfo(androidSdkHome))
		{
		}

		internal override string SdkPackageId => "platform-tools";

		public override FileInfo FindToolPath(DirectoryInfo androidSdkHome)
			=> FindTool(androidSdkHome, toolName: "adb", windowsExtension: ".exe", "platform-tools");

		AdbRunner runner;

		public List<AdbDevice> GetDevices()
		{
			var devices = new List<AdbDevice>();

			//adb devices -l
			var builder = new ProcessArgumentBuilder();

			builder.Append("devices");
			builder.Append("-l");

			var r = runner.RunAdb(AndroidSdkHome, builder);

			if (r.StandardOutput.Count > 1)
			{
				foreach (var line in r.StandardOutput?.Skip(1))
				{
					var parts = Regex.Split(line, "\\s+");

					var d = new AdbDevice
					{
						Serial = parts[0].Trim()
					};

					if (parts.Length > 1 && (parts[1]?.ToLowerInvariant() ?? "offline") == "offline")
						continue;

					if (parts.Length > 2)
					{
						foreach (var part in parts.Skip(2))
						{
							var bits = part.Split(new[] { ':' }, 2);
							if (bits == null || bits.Length != 2)
								continue;

							switch (bits[0].ToLower())
							{
								case "usb":
									d.Usb = bits[1];
									break;
								case "product":
									d.Product = bits[1];
									break;
								case "model":
									d.Model = bits[1];
									break;
								case "device":
									d.Device = bits[1];
									break;
							}
						}
					}

					if (!string.IsNullOrEmpty(d?.Serial))
						devices.Add(d);
				}
			}

			return devices;
		}
		public ShellProcessRunner.ShellProcessResult RunCommand(string command, params string[] parameters)
		{
			var builder = new ProcessArgumentBuilder();

			builder.Append(command);
			if (parameters != null)
				foreach (var p in parameters)
					builder.Append(p);

			return runner.RunAdb(AndroidSdkHome, builder);
		}

		public void KillServer()
		{
			//adb kill-server
			var builder = new ProcessArgumentBuilder();

			builder.Append("kill-server");

			runner.RunAdb(AndroidSdkHome, builder);
		}

		public void StartServer()
		{
			//adb kill-server
			var builder = new ProcessArgumentBuilder();

			builder.Append("start-server");

			runner.RunAdb(AndroidSdkHome, builder);
		}

		public void Connect(string deviceIp, int port = 5555)
		{
			// adb connect device_ip_address:5555
			var builder = new ProcessArgumentBuilder();

			builder.Append("connect");
			builder.Append(deviceIp + ":" + port);

			runner.RunAdb(AndroidSdkHome, builder);
		}

		public void Disconnect(string deviceIp = null, int? port = null)
		{
			// adb connect device_ip_address:5555
			var builder = new ProcessArgumentBuilder();

			builder.Append("disconnect");
			if (!string.IsNullOrEmpty(deviceIp))
				builder.Append(deviceIp + ":" + (port ?? 5555));

			runner.RunAdb(AndroidSdkHome, builder);
		}

		public void Install(FileInfo apkFile, string adbSerial = null)
		{
			// adb uninstall -k <package>
			// -k keeps data & cache dir
			var builder = new ProcessArgumentBuilder();

			runner.AddSerial(adbSerial, builder);

			builder.Append("install");
			builder.Append(apkFile.FullName);

			runner.RunAdb(AndroidSdkHome, builder);
		}

		public void WaitFor(AdbTransport transport = AdbTransport.Any, AdbState state = AdbState.Device, string adbSerial = null)
		{
			// adb wait-for[-<transport>]-<state>
			//  transport: usb, local, or any (default)
			//  state: device, recovery, sideload, bootloader
			var builder = new ProcessArgumentBuilder();

			runner.AddSerial(adbSerial, builder);

			var x = "wait-for";
			if (transport == AdbTransport.Local)
				x = "-local";
			else if (transport == AdbTransport.Usb)
				x = "-usb";

			switch (state)
			{
				case AdbState.Bootloader:
					x += "-bootloader";
					break;
				case AdbState.Device:
					x += "-device";
					break;
				case AdbState.Recovery:
					x += "-recovery";
					break;
				case AdbState.Sideload:
					x += "-sideload";
					break;
			}

			builder.Append(x);

			runner.RunAdb(AndroidSdkHome, builder);
		}

		public void Uninstall(string packageName, bool keepDataAndCacheDirs = false, string adbSerial = null)
		{
			// adb uninstall -k <package>
			// -k keeps data & cache dir
			var builder = new ProcessArgumentBuilder();

			runner.AddSerial(adbSerial, builder);

			builder.Append("uninstall");
			if (keepDataAndCacheDirs)
				builder.Append("-k");
			builder.Append(packageName);

			runner.RunAdb(AndroidSdkHome, builder);
		}

		public bool EmuKill(string adbSerial = null)
		{
			// adb uninstall -k <package>
			// -k keeps data & cache dir
			var builder = new ProcessArgumentBuilder();

			runner.AddSerial(adbSerial, builder);

			builder.Append("emu");
			builder.Append("kill");

			var r = runner.RunAdb(AndroidSdkHome, builder);

			return r.StandardOutput.Any(o => o.ToLowerInvariant().Contains("stopping emulator"));
		}

		public string EmuAvdName(string adbSerial = null)
		{
			// adb uninstall -k <package>
			// -k keeps data & cache dir
			var builder = new ProcessArgumentBuilder();

			runner.AddSerial(adbSerial, builder);

			builder.Append("emu");
			builder.Append("avd");
			builder.Append("name");

			var r = runner.RunAdb(AndroidSdkHome, builder);

			return r?.StandardOutput?.FirstOrDefault()?.Trim();
		}

		public bool Pull(FileInfo remoteFileSource, FileInfo localFileDestination, string adbSerial = null)
			=> pull(remoteFileSource.FullName, localFileDestination.FullName, adbSerial);


		public bool Pull(DirectoryInfo remoteDirectorySource, DirectoryInfo localDirectoryDestination, string adbSerial = null)
			=> pull(remoteDirectorySource.FullName, localDirectoryDestination.FullName, adbSerial);

		public bool Pull(FileInfo remoteFileSource, DirectoryInfo localDirectoryDestination, string adbSerial = null)
			=> pull(remoteFileSource.FullName, localDirectoryDestination.FullName, adbSerial);

		bool pull(string remoteSrc, string localDest, string adbSerial = null)
		{
			// adb uninstall -k <package>
			// -k keeps data & cache dir
			var builder = new ProcessArgumentBuilder();

			runner.AddSerial(adbSerial, builder);

			builder.Append("pull");
			builder.AppendQuoted(remoteSrc);
			builder.AppendQuoted(localDest);

			var r = runner.RunAdb(AndroidSdkHome, builder);

			return r.Success;
		}

		public bool Push(FileInfo localFileSource, FileInfo remoteFileDestination, string adbSerial = null)
			=> push(localFileSource.FullName, remoteFileDestination.FullName, adbSerial);

		public bool Push(FileInfo localFileSource, DirectoryInfo remoteDirectoryDestination, string adbSerial = null)
			=> push(localFileSource.FullName, remoteDirectoryDestination.FullName, adbSerial);

		public bool Push(DirectoryInfo localDirectorySource, DirectoryInfo remoteDirectoryDestination, string adbSerial = null)
			=> push(localDirectorySource.FullName, remoteDirectoryDestination.FullName, adbSerial);

		bool push(string localSrc, string remoteDest, string adbSerial = null)
		{
			// adb uninstall -k <package>
			// -k keeps data & cache dir
			var builder = new ProcessArgumentBuilder();

			runner.AddSerial(adbSerial, builder);

			builder.Append("pull");
			builder.AppendQuoted(localSrc);
			builder.AppendQuoted(remoteDest);

			var r = runner.RunAdb(AndroidSdkHome, builder);
			return r.Success;
		}

		public List<string> BugReport(string adbSerial = null)
		{
			// adb uninstall -k <package>
			// -k keeps data & cache dir
			var builder = new ProcessArgumentBuilder();

			runner.AddSerial(adbSerial, builder);

			builder.Append("bugreport");

			var r = runner.RunAdb(AndroidSdkHome, builder);

			return r.StandardOutput;
		}


		public List<string> Logcat(AdbLogcatOptions options = null, string adbSerial = null)
		{
			// logcat[option][filter - specs]
			if (options == null)
				options = new AdbLogcatOptions();

			// adb uninstall -k <package>
			// -k keeps data & cache dir
			var builder = new ProcessArgumentBuilder();

			runner.AddSerial(adbSerial, builder);

			builder.Append("logcat");

			if (options.BufferType != AdbLogcatBufferType.Main)
			{
				builder.Append("-b");
				builder.Append(options.BufferType.ToString().ToLowerInvariant());
			}

			if (options.Clear || options.PrintSize)
			{
				if (options.Clear)
					builder.Append("-c");
				else if (options.PrintSize)
					builder.Append("-g");
			}
			else
			{
				// Always dump, since we want to return and not listen to logcat forever
				// in the future might be nice to add an alias that takes a cancellation token
				// and can pipe output until that token is cancelled.
				//if (options.Dump)
				builder.Append("-d");

				if (options.OutputFile != null)
				{
					builder.Append("-f");
					builder.AppendQuoted(options.OutputFile.FullName);

					if (options.NumRotatedLogs.HasValue)
					{
						builder.Append("-n");
						builder.Append(options.NumRotatedLogs.Value.ToString());
					}

					var kb = options.LogRotationKb ?? 16;
					builder.Append("-r");
					builder.Append(kb.ToString());
				}

				if (options.SilentFilter)
					builder.Append("-s");

				if (options.Verbosity != AdbLogcatOutputVerbosity.Brief)
				{
					builder.Append("-v");
					builder.Append(options.Verbosity.ToString().ToLowerInvariant());
				}

			}

			var r = runner.RunAdb(AndroidSdkHome, builder);

			return r.StandardOutput;
		}

		public string Version()
		{
			// adb version
			var builder = new ProcessArgumentBuilder();

			builder.Append("version");
			var output = new List<string>();
			var r = runner.RunAdb(AndroidSdkHome, builder);

			return string.Join(Environment.NewLine, r.StandardOutput);
		}

		public string GetSerialNumber(string adbSerial = null)
		{
			// adb uninstall -k <package>
			// -k keeps data & cache dir
			var builder = new ProcessArgumentBuilder();

			runner.AddSerial(adbSerial, builder);

			builder.Append("get-serialno");

			var r = runner.RunAdb(AndroidSdkHome, builder);

			return string.Join(Environment.NewLine, r.StandardOutput);
		}

		public string GetState(string adbSerial = null)
		{
			// adb uninstall -k <package>
			// -k keeps data & cache dir
			var builder = new ProcessArgumentBuilder();

			runner.AddSerial(adbSerial, builder);

			builder.Append("get-state");

			var r = runner.RunAdb(AndroidSdkHome, builder);

			return string.Join(Environment.NewLine, r.StandardOutput);
		}

		public Dictionary<string, string> GetProperties(string adbSerial = null, params string[] includeProperties)
		{
			var r = new Dictionary<string, string>();

			var lines = Shell("getprop", adbSerial);

			foreach (var l in lines)
			{
				if (l?.Contains(':') ?? false)
				{
					var parts = l.Split(new[] { ':' }, 2);
					if (parts != null && parts.Length == 2)
					{
						var key = parts[0].Trim().Trim('[', ']');

						if (includeProperties == null || (includeProperties?.Any(ip => ip.Equals(key, StringComparison.OrdinalIgnoreCase)) ?? false))
							r[key] = parts[1].Trim().Trim('[', ']');
					}
				}
			}

			return r;
		}

		public List<string> Shell(string shellCommand, string adbSerial = null)
		{
			// adb uninstall -k <package>
			// -k keeps data & cache dir
			var builder = new ProcessArgumentBuilder();

			runner.AddSerial(adbSerial, builder);

			builder.Append("shell");
			builder.Append(shellCommand);

			var output = new List<string>();

			var r = runner.RunAdb(AndroidSdkHome, builder);

			return r.StandardOutput;
		}


		public void ScreenCapture(FileInfo saveToLocalFile, string adbSerial = null)
		{
			//adb shell screencap / sdcard / screen.png
			var guid = Guid.NewGuid().ToString();
			var remoteFile = "/sdcard/" + guid + ".png";

			Shell("screencap " + remoteFile, adbSerial);

			Pull(new FileInfo(remoteFile), saveToLocalFile, adbSerial);

			Shell("rm " + remoteFile, adbSerial);
		}

		public void ScreenRecord(FileInfo saveToLocalFile, System.Threading.CancellationToken? recordingCancelToken = null, TimeSpan? timeLimit = null, int? bitrateMbps = null, int? width = null, int? height = null, bool rotate = false, bool logVerbose = false, string adbSerial = null)
		{
			// screenrecord[options] filename

			var guid = Guid.NewGuid().ToString();
			var remoteFile = "/sdcard/" + guid + ".mp4";

			// adb uninstall -k <package>
			// -k keeps data & cache dir
			var builder = new ProcessArgumentBuilder();

			runner.AddSerial(adbSerial, builder);

			builder.Append("shell");
			builder.Append("screenrecord");

			if (timeLimit.HasValue)
			{
				builder.Append("--time-limit");
				builder.Append(((int)timeLimit.Value.TotalSeconds).ToString());
			}

			if (bitrateMbps.HasValue)
			{
				builder.Append("--bit-rate");
				builder.Append((bitrateMbps.Value * 1000000).ToString());
			}

			if (width.HasValue && height.HasValue)
			{
				builder.Append("--size");
				builder.Append($"{width}x{height}");
			}

			if (rotate)
				builder.Append("--rotate");

			if (logVerbose)
				builder.Append("--verbose");

			builder.Append(remoteFile);

			if (recordingCancelToken.HasValue)
				runner.RunAdb(AndroidSdkHome, builder, recordingCancelToken.Value);
			else
				runner.RunAdb(AndroidSdkHome, builder);

			Pull(new FileInfo(remoteFile), saveToLocalFile, adbSerial);

			Shell("rm " + remoteFile, adbSerial);
		}

		public string GetDeviceName(string adbSerial = null)
		{
			try
			{
				return GetEmulatorName(adbSerial);
			}
			catch (InvalidDataException)
			{
				// Shell getprop
				var s = Shell("getprop ro.product.model", adbSerial);

				if (s?.Any() ?? false)
					return s.FirstOrDefault().Trim();

				// Shell getprop
				s = Shell("getprop ro.product.name", adbSerial);

				if (s?.Any() ?? false)
					return s.FirstOrDefault().Trim();
			}

			return null;
		}

		public string GetEmulatorName(string adbSerial = null)
		{
			var shellName = EmuAvdName(adbSerial);

			if (!string.IsNullOrWhiteSpace(shellName))
				return shellName;

			if (string.IsNullOrEmpty(adbSerial) || !adbSerial.StartsWith("emulator-", StringComparison.OrdinalIgnoreCase))
				throw new InvalidDataException("Serial must be an emulator starting with `emulator-`");

			int port = 5554;
			if (!int.TryParse(adbSerial.Substring(9), out port))
				return null;

			var tcpClient = new System.Net.Sockets.TcpClient("127.0.0.1", port);
			var name = string.Empty;
			using (var s = tcpClient.GetStream())
			{

				System.Threading.Thread.Sleep(250);

				foreach (var b in Encoding.ASCII.GetBytes("avd name\r\n"))
					s.WriteByte(b);

				System.Threading.Thread.Sleep(250);

				byte[] data = new byte[1024];
				using (var memoryStream = new MemoryStream())
				{
					do
					{
						var len = s.Read(data, 0, data.Length);
						memoryStream.Write(data, 0, len);
					} while (s.DataAvailable);

					var txt = Encoding.ASCII.GetString(memoryStream.ToArray(), 0, (int)memoryStream.Length);

					var m = Regex.Match(txt, "OK(?<name>.*?)OK", RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline);
					name = m?.Groups?["name"]?.Value?.Trim();
				}
			}

			return name;
		}

		public IEnumerable<string> LaunchApp(string packageName, string adbSerial = null)
		{
			// Use a trick to have monkey launch the app by package name
			// so we don't need know the activity class for the main launcher
			return Shell("monkey -p {packageName} -v 1", adbSerial);
		}
	}
}