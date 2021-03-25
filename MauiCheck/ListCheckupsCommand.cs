using DotNetCheck.Models;
using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DotNetCheck
{
	public class ListCheckupCommand : AsyncCommand<ListCheckupSettings>
	{
		public override async Task<int> ExecuteAsync(CommandContext context, ListCheckupSettings settings)
		{
			var manifest = await Manifest.Manifest.FromFileOrUrl(settings.Manifest);

			if (!ToolInfo.Validate(manifest))
				return -1;

			var checkups = CheckupManager.BuildCheckupGraph(manifest);

			foreach (var c in checkups)
			{
				AnsiConsole.WriteLine(c.GetType().Name.ToString() + " (" + c.Id + ")");
			}

			if (!settings.NonInteractive)
			{
				AnsiConsole.WriteLine();
				AnsiConsole.WriteLine("Press enter to exit...");
				Console.ReadLine();
			}

			return 0;
		}

	}
}
