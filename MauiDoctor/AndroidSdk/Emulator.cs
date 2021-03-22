using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.IO;
using System.Threading;
using System.Data;

namespace MauiDoctor.AndroidSdk
{
	public partial class Emulator : SdkTool
	{
		public Emulator()
			: this((DirectoryInfo)null)
		{ }

		public Emulator(DirectoryInfo androidSdkHome)
			: base(androidSdkHome)
		{
			AndroidSdkHome = androidSdkHome;
		}

		public Emulator(string androidSdkHome)
			: this(new DirectoryInfo(androidSdkHome))
		{ }

		internal override string SdkPackageId => "emulator";

		public override FileInfo FindToolPath(DirectoryInfo androidSdkHome)
			=> FindTool(androidSdkHome, toolName: "emulator", windowsExtension: ".exe", "emulator");

		public IEnumerable<string> ListAvds()
		{
			var builder = new ProcessArgumentBuilder();

			builder.Append("-list-avds");

			var p = Run(builder);

			foreach (var l in p)
			{
				if (!string.IsNullOrWhiteSpace(l))
					yield return l;
			}
		}

		public AndroidEmulatorProcess Start(string avdName, EmulatorStartOptions options = null)
		{
			if (options == null)
				options = new EmulatorStartOptions();

			var builder = new ProcessArgumentBuilder();

			builder.Append($"-avd {avdName}");

			if (options.NoSnapshotLoad)
				builder.Append("-no-snapshot-load");
			if (options.NoSnapshotSave)
				builder.Append("-no-snapshot-save");
			if (options.NoSnapshot)
				builder.Append("-no-snapshot");

			if (!string.IsNullOrEmpty(options.CameraBack))
				builder.Append($"-camera-back {options.CameraBack}");
			if (!string.IsNullOrEmpty(options.CameraFront))
				builder.Append($"-camera-front {options.CameraFront}");

			if (options.MemoryMegabytes.HasValue)
				builder.Append($"-memory {options.MemoryMegabytes}");

			if (options.SdCard != null)
			{
				builder.Append("-sdcard");
				builder.AppendQuoted(options.SdCard.FullName);
			}

			if (options.WipeData)
				builder.Append("-wipe-data");

			if (options.Debug != null && options.Debug.Length > 0)
				builder.Append("-debug " + string.Join(",", options.Debug));

			if (options.Logcat != null && options.Logcat.Length > 0)
				builder.Append("-logcat " + string.Join(",", options.Logcat));

			if (options.ShowKernel)
				builder.Append("-show-kernel");

			if (options.Verbose)
				builder.Append("-verbose");

			if (options.DnsServers != null && options.DnsServers.Length > 0)
				builder.Append("-dns-server " + string.Join(",", options.DnsServers));

			if (!string.IsNullOrEmpty(options.HttpProxy))
				builder.Append($"-http-proxy {options.HttpProxy}");

			if (!string.IsNullOrEmpty(options.NetDelay))
				builder.Append($"-netdelay {options.NetDelay}");

			if (options.NetFast)
				builder.Append("-netfast");

			if (!string.IsNullOrEmpty(options.NetSpeed))
				builder.Append($"-netspeed {options.NetSpeed}");

			if (options.Ports.HasValue)
				builder.Append($"-ports {options.Ports.Value.console},{options.Ports.Value.adb}");
			else if (options.Port.HasValue)
				builder.Append($"-port {options.Port.Value}");

			if (options.TcpDump != null)
			{
				builder.Append("-tcpdump");
				builder.AppendQuoted(options.TcpDump.FullName);
			}

			if (options.Acceleration.HasValue)
				builder.Append($"-accel {options.Acceleration.Value.ToString().ToLowerInvariant()}");

			if (options.NoAccel)
				builder.Append("-no-accel");

			if (options.Engine.HasValue)
				builder.Append($"-engine {options.Engine.Value.ToString().ToLowerInvariant()}");

			if (options.NoJni)
				builder.Append("-no-jni");

			if (options.SeLinux.HasValue)
				builder.Append($"-selinux {options.SeLinux.Value.ToString().ToLowerInvariant()}");

			if (!string.IsNullOrEmpty(options.Timezone))
				builder.Append($"-timezone {options.Timezone}");

			if (options.NoBootAnim)
				builder.Append("-no-boot-anim");

			if (options.Screen.HasValue)
				builder.Append($"-screen {options.Screen.Value.ToString().ToLowerInvariant()}");

			//var uuid = Guid.NewGuid().ToString("D");
			//builder.Append($"-prop emu.uuid={uuid}");

			if (options.ExtraArgs != null && options.ExtraArgs.Length > 0)
			{
				foreach (var arg in options.ExtraArgs)
					builder.Append(arg);
			}

			return new AndroidEmulatorProcess(Start(builder), avdName, AndroidSdkHome);
		}

		IEnumerable<string> Run(ProcessArgumentBuilder builder, params string[] args)
		{
			var p = Start(builder, args);

			var r = p.WaitForExit();

			return r.StandardOutput;
		}

		ShellProcessRunner Start(ProcessArgumentBuilder builder, params string[] args)
		{
			var emulator = FindToolPath(AndroidSdkHome);
			if (emulator == null || !File.Exists(emulator.FullName))
				throw new FileNotFoundException("Could not find emulator", emulator?.FullName);

			if (args != null && args.Length > 0)
			{
				foreach (var arg in args)
					builder.Append(arg);
			}

			var p = new ShellProcessRunner(new ShellProcessRunnerOptions(emulator.FullName, builder.ToString()));
			return p;
		}

		public class AndroidEmulatorProcess
		{
			internal AndroidEmulatorProcess(ShellProcessRunner p, string avdName, DirectoryInfo sdkHome)
			{
				process = p;
				androidSdkHome = sdkHome;
				AvdName = avdName;
			}

			readonly ShellProcessRunner process;
			readonly DirectoryInfo androidSdkHome;
			ShellProcessRunner.ShellProcessResult result;

			public string Serial { get; private set; }

			public string AvdName { get; private set; }

			public int WaitForExit()
			{
				result = process.WaitForExit();

				return result.ExitCode;
			}

			public bool Shutdown()
			{
				var success = false;

				if (!string.IsNullOrWhiteSpace(Serial))
				{
					var adb = new Adb(androidSdkHome);

					try { success = adb.EmuKill(Serial); }
					catch { }
				}

				if (process != null && !process.HasExited)
				{
					try { process.Kill(); success = true; }
					catch { }
				}

				return success;
			}

			public IEnumerable<string> GetStandardOutput()
				=> result?.StandardOutput ?? new List<string>();


			public bool WaitForBootComplete()
				=> WaitForBootComplete(TimeSpan.Zero);

			public bool WaitForBootComplete(TimeSpan timeout)
			{
				var cts = new CancellationTokenSource();

				if (timeout != TimeSpan.Zero)
					cts.CancelAfter(timeout);

				return WaitForBootComplete(cts.Token);
			}

			public bool WaitForBootComplete(CancellationToken token)
			{
				var adb = new Adb(androidSdkHome);

				var booted = false;
				Serial = null;

				// Keep trying to see if the boot complete prop is set
				while (string.IsNullOrEmpty(Serial) && !token.IsCancellationRequested)
				{
					if (process.HasExited)
						return false;

					Thread.Sleep(1000);

					// Get a list of devices, we need to find the device we started
					var devices = adb.GetDevices();

					// Find the device we just started and get it's adb serial
					foreach (var d in devices)
					{
						try
						{
							var name = adb.GetEmulatorName(d.Serial);
							if (name.Equals(AvdName, StringComparison.OrdinalIgnoreCase))
							{
								Serial = d.Serial;
								break;
							}
						}
						catch { }
					}
				}

				while (!token.IsCancellationRequested)
				{
					if (process.HasExited)
						return false;

					if (adb.Shell("getprop dev.bootcomplete", Serial).Any(l => l.Contains("1")))
					{
						booted = true;
						break;
					}
					else
					{
						Thread.Sleep(1000);
					}
				}

				return booted;
			}
		}
	}
}