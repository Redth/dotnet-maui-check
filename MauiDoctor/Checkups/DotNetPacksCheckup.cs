using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MauiDoctor.Doctoring;
using Microsoft.NET.Sdk.WorkloadManifestReader;
using NuGet.Versioning;

namespace MauiDoctor.Checkups
{
	public class DotNetPacksCheckup : Checkup
	{
		public DotNetPacksCheckup(string sdkVersion, params Manifest.DotNetPack[] requiredPacks) : base()
		{
			SdkVersion = sdkVersion;
			RequiredPacks = requiredPacks?.Where(p => p.SupportedFor(Util.Platform))?.ToArray();
		}

		public override string[] Dependencies => new string[] { "dotnet" };

		
		public Manifest.DotNetPack[] RequiredPacks { get; }
		public string SdkVersion { get; }


		public override string Id => "dotnetpacks";

		public override string Title => $".NET Core SDK Packs ({SdkVersion})";

		public override async Task<Diagonosis> Examine()
		{
			var dn = new DotNet();
			var sdk = (await dn.GetSdks())?.FirstOrDefault(s => s.Version.Equals(SdkVersion));

			var sdkPacks = await dn.GetWorkloadPacks(SdkVersion, WorkloadPackKind.Sdk);


			var missingPacks = new List<Manifest.DotNetPack>();

			foreach (var rp in RequiredPacks)
			{
				if (!sdkPacks.Any(sp => sp.Id == rp.Id && sp.Version == rp.Version))
				{
					ReportStatus($":warning: {rp.Id} ({rp.Version}) not found.");
					missingPacks.Add(rp);
				}
				else
				{
					ReportStatus($":check_mark: [darkgreen]{rp.Id} ({rp.Version}) found.[/]");
				}
			}

			if (!missingPacks.Any())
				return Diagonosis.Ok(this);

			return new Diagonosis(
				Status.Error,
				this,
				new Prescription("Install Missing SDK Packs",
					new BootsRemedy(missingPacks.Select(mp => (mp.Urls.For(Util.Platform)?.ToString(), $"{mp.Id} ({mp.Version})")).ToArray())));
		}
	}
}
