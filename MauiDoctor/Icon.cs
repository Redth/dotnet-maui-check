namespace MauiDoctor
{
	public static class Icon
	{
		public static string PrettyBoring(string pretty, string boring)
			=> Util.IsWindows ? boring : pretty;

		public static string Error
			=> PrettyBoring(":cross_mark:", "×");
		public static string Warning
			=> PrettyBoring(":warning:", "¡");

		public static string ListItem
			=> "–";

		public static string Checking
			=> PrettyBoring(":magnifying_glass_tilted_right:", "›");
		public static string Recommend
			=> PrettyBoring(":syringe:", "¤");

		public static string Success
			=> PrettyBoring(":check_mark:", "–");
	}


}
