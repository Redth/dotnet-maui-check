namespace MauiDoctor.AndroidSdk
{
	public partial class AvdManager
	{
		/// <summary>
		/// AVD Device Info
		/// </summary>
		public class AvdDevice
		{
			/// <summary>
			/// Gets or sets the Device name.
			/// </summary>
			/// <value>The name.</value>
			public string Name { get; set; }

			public string Id { get; set; }

			public string Oem { get; set; }

			public override string ToString()
			{
				return $"{Id} | {Name} | {Oem}";
			}
		}
	}
}
