using System;

namespace DotNetCheck.Models
{
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
