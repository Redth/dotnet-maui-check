namespace DotNetCheck.AndroidSdk
{
	public partial class AvdManager
	{
		/// <summary>
		/// AVD Target Info
		/// </summary>
		public class AvdTarget
		{
			/// <summary>
			/// Gets or sets the AVD target identifier.
			/// </summary>
			/// <value>The identifier.</value>
			public string Id { get; set; }

			public string Name { get; set; }

			public string Type { get; set; }

			public int ApiLevel { get; set; }

			public int Revision { get; set; }

			public override string ToString()
			{
				return $"{Id} | {Name} | {Type} | {ApiLevel} | {Revision}";
			}
		}
	}
}
