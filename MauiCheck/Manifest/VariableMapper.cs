using System.Collections.Generic;

namespace DotNetCheck.Manifest
{
	public abstract class VariableMapper
	{
		public Dictionary<string, object> Variables = new Dictionary<string, object>();

		public abstract void Map();
	}
}
