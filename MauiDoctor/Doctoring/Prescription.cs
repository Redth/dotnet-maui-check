namespace MauiDoctor.Doctoring
{
	public class Prescription
	{
		public Prescription(string name)
			: this(name, (string)null, null)
		{
		}

		public Prescription(string name, params Remedy[] remedies)
			: this(name, null, remedies)
		{
		}

		public Prescription(string name, string description)
			: this(name, description, null)
		{
		}

		public Prescription(string name, string description, params Remedy[] remedies)
		{
			Name = name;
			Description = description;
			Remedies = remedies;
		}

		public string Name { get; private set; }

		public string Description { get; private set; }

		public bool HasRemedy
			=> Remedies != null && Remedies.Length > 0;

		public virtual Remedy[] Remedies { get; set; }
	}
}
