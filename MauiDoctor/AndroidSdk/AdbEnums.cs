using System;
using System.Collections.Generic;
using System.Text;

namespace MauiDoctor.AndroidSdk
{
	public partial class Adb
	{

		public enum AdbTransport
		{
			Any = 0,
			Usb = 1,
			Local = 2
		}

		public enum AdbState
		{
			Device = 0,
			Recovery,
			Sideload,
			Bootloader
		}

		/// <summary>
		/// Which Logcat buffer to return.
		/// </summary>
		public enum AdbLogcatBufferType
		{
			/// <summary>
			/// Main - Main buffer.
			/// </summary>
			Main = 0,
			/// <summary>
			/// Radio - View the buffer that contains radio/telephony related messages.
			/// </summary>
			Radio = 1,
			/// <summary>
			/// Events - View the buffer containing events-related messages.
			/// </summary>
			Events = 2
		}

		/// <summary>
		/// Verbosity of Logcat output to return.
		/// </summary>
		public enum AdbLogcatOutputVerbosity
		{
			/// <summary>
			/// Brief - Display priority/tag and PID of the process issuing the message (the default format).
			/// </summary>
			Brief = 0,
			/// <summary>
			/// Process - Display PID only.
			/// </summary>
			Process,
			/// <summary>
			/// Tag - Display the priority/tag only.
			/// </summary>
			Tag,
			/// <summary>
			/// Raw - Display the raw log message, with no other metadata fields.
			/// </summary>
			Raw,
			/// <summary>
			/// Time - Display the date, invocation time, priority/tag, and PID of the process issuing the message.
			/// </summary>
			Time,
			/// <summary>
			/// ThreadTime - Display the date, invocation time, priority, tag, and the PID and TID of the thread issuing the message.
			/// </summary>
			ThreadTime,
			/// <summary>
			/// Long - Display all metadata fields and separate messages with blank lines.
			/// </summary>
			Long,
		}
	}
}