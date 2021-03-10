using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MauiDoctor.Checkups;
using MauiDoctor.Cli;
using MauiDoctor.Doctoring;
using NuGet.Versioning;
using Spectre.Console;
using Spectre.Console.Cli;

namespace MauiDoctor
{
	class Program
	{
		static async Task<int> Main(string[] args)
		{
			var app = new CommandApp();

			app.Configure(config =>
			{
				config.AddCommand<DoctorCommand>("doctor");
			});

			return app.Run(new[] { "doctor" }.Concat(args));
		}
	}
}
