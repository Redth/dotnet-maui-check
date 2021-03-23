using System;

namespace DotNetCheck.Models
{
	public class CheckupStatusEventArgs : EventArgs
	{
		public CheckupStatusEventArgs(Checkup checkup, string message, Status? status = null, int progress = -1)
			: base()
		{
			Checkup = checkup;
			Message = message;
			Progress = progress;
			Status = status;
		}

		public Status? Status { get; private set; }

		public Checkup Checkup { get; private set; }
		public string Message { get; private set; }
		public int Progress { get; private set; }
	}

	public class RemedyStatusEventArgs : EventArgs
	{
		public RemedyStatusEventArgs(Solution remedy, string message, int progress = -1)
			: base()
		{
			Remedy = remedy;
			Message = message;
			Progress = progress;
		}

		public Solution Remedy { get; private set; }
		public string Message { get; private set; }
		public int Progress { get; private set; }
	}
}
