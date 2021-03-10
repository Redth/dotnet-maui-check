namespace MauiDoctor.Doctoring
{
	public class Diagonosis
	{
		public static Diagonosis Ok(Checkup checkup)
			=> new Diagonosis(Status.Ok, checkup);

		public Diagonosis(Status status, Checkup checkup, string message)
			: this(status, checkup, message, null)
		{
		}

		public Diagonosis(Status status, Checkup checkup, Prescription prescription)
			: this(status, checkup, null, prescription)
		{
		}

		public Diagonosis(Status status, Checkup checkup, string message = null, Prescription prescription = null)
		{
			Status = status;
			Checkup = checkup;
			Message = message;
			Prescription = prescription;
		}

		public Status Status { get; private set; }
		public Checkup Checkup { get; private set; }

		public virtual string Message { get; private set; }

		public bool HasPrescription => Prescription != null;

		public virtual Prescription Prescription { get; private set; }
	}
}
