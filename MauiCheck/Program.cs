using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotNetCheck.Checkups;
using DotNetCheck.Cli;
using DotNetCheck.Models;
using NuGet.Versioning;
using Spectre.Console;
using Spectre.Console.Cli;

namespace DotNetCheck
{
	class Program
	{
		static Task<int> Main(string[] args)
		{
			// Need to register the code pages provider for code that parses
			// and later needs ISO-8859-2
			System.Text.Encoding.RegisterProvider(
				System.Text.CodePagesEncodingProvider.Instance);
			// Test that it loads
			_ = System.Text.Encoding.GetEncoding("ISO-8859-2");

			var app = new CommandApp();

			app.Configure(config =>
			{
				config.AddCommand<CheckCommand>("check");
			});

			return app.RunAsync(new[] { "check" }.Concat(args));
		}
	}
}
