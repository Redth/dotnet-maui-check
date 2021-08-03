using System.Collections.Generic;
using System.Threading.Tasks;

namespace DotNetCheck.Manifest
{
	public abstract class VariableMapper
	{
		public Dictionary<string, object> Variables = new Dictionary<string, object>();

		public abstract Task Map();
	}
}
