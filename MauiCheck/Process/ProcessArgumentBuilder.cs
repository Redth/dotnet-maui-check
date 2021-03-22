using System;
using System.Collections.Generic;
using System.Text;

namespace DotNetCheck
{
	internal class ProcessArgumentBuilder
	{
		public List<string> args = new List<string>();

		readonly string quote = Util.IsWindows ? "\"" : "'";

		public void Append(string arg)
			=> args.Add(arg);

		public void AppendQuoted(string arg)
			=> args.Add($"{quote}{arg}{quote}");

		public override string ToString()
			=> string.Join(" ", args);
	}
}
