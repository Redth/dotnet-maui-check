namespace DotNetCheck.Models
{
	public class DiagnosticResult
	{
		public static DiagnosticResult Ok(Checkup checkup)
			=> new DiagnosticResult(Status.Ok, checkup);

		public DiagnosticResult(Status status, Checkup checkup, string message)
			: this(status, checkup, message, null)
		{
		}

		public DiagnosticResult(Status status, Checkup checkup, Suggestion prescription)
			: this(status, checkup, null, prescription)
		{
		}

		public DiagnosticResult(Status status, Checkup checkup, string message = null, Suggestion prescription = null)
		{
			Status = status;
			Checkup = checkup;
			Message = message;
			Suggestion = prescription;
		}

		public Status Status { get; private set; }
		public Checkup Checkup { get; private set; }

		public virtual string Message { get; private set; }

		public bool HasSuggestion => Suggestion != null;

		public virtual Suggestion Suggestion { get; private set; }
	}
}
