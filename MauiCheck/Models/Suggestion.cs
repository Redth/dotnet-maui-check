namespace DotNetCheck.Models
{
	public class Suggestion
	{
		public Suggestion(string name)
			: this(name, (string)null, null)
		{
		}

		public Suggestion(string name, params Solution[] remedies)
			: this(name, null, remedies)
		{
		}

		public Suggestion(string name, string description)
			: this(name, description, null)
		{
		}

		public Suggestion(string name, string description, params Solution[] remedies)
		{
			Name = name;
			Description = description;
			Solutions = remedies;
		}

		public string Name { get; private set; }

		public string Description { get; private set; }

		public bool HasSolution
			=> Solutions != null && Solutions.Length > 0;

		public virtual Solution[] Solutions { get; set; }
	}
}
