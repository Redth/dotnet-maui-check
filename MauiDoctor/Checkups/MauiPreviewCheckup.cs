using System;
using System.Threading.Tasks;
using MauiDoctor.Doctoring;

namespace MauiDoctor.Checkups
{
	public class MauiCheckup : Doctoring.Checkup
	{
		public MauiCheckup()
		{
		}

		public override string Id => "maui";

		public override string Title => ".NET MAUI";


		public override string[] Dependencies => base.Dependencies;

		public override Task<Diagonosis> Examine()
		{
			throw new NotImplementedException();
		}
	}
}
