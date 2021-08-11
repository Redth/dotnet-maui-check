param (
	[Parameter(HelpMessage="DotNet SDK Root install directory")]
		[string]$DotnetRoot = $env:DOTNET_ROOT,
	[Parameter(HelpMessage="Deletes SDK/Runtime files as well - only use this if you know what you are doing!")]
		[switch]$FullDelete = $false
)

$requireAdmin = $false

# Otherwise look for the default paths
if (-not ($DotnetRoot))
{
	# Admin / sudo required for global installs
	$requireAdmin = $true

	if ($IsMacOS)
	{
		$DotnetRoot = '/usr/local/share/dotnet/'
	} elseif ($IsWindows)
	{
		$DotnetRoot = Join-Path -Path $env:ProgramFiles -ChildPath 'dotnet'
	}
}

# Make sure the SDK path exists
if (-not (Test-Path $DotnetRoot))
{
	throw "dotnet SDK root not found"
}

# If modifying a global install we need admin/sudo, so check the context if we are
if ($requireAdmin)
{
	if ($IsWindows)
	{
		if (-NOT ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator"))
		{
			throw "Adminstrator privilege required to modify global dotnet install.  Re-run this script under an elevated terminal."
		}
	}
	else
	{
		if (-NOT ($(whoami) -eq 'root'))
		{
			throw "Superuser privilege required to modify global dotnet install.  Re-run this script with sudo."
		}
	}
}

$rmPaths = @(
	'packs/Microsoft.Android.*',
	'packs/Microsoft.iOS.*',
	'packs/Microsoft.MacCatalyst.*',
	'packs/Microsoft.macOS.*',
	'packs/Microsoft.Maui.*',
	'packs/Microsoft.NET.Runtime.*',
	'packs/Microsoft.NETCore.App.Runtime.AOT.*',
	'packs/Microsoft.NETCore.App.Runtime.Mono.*',
	'packs/Microsoft.tvOS.*',
	'templates/6.0*',
	'metadata/*'
)

# Delete ALL dotnet6 preview files including host runtime and sdk
if ($FullDelete)
{
	# macOS stores some shared bits in a .app file and windows does not use this extension
	$osAppExt = ''
	if ($IsMacOS)
	{
		$osAppExt = '.app'
	}

	$rmPaths += @(
		'sdk-manifests/6.0.*',
		'host/fxr/6.0*',
		'sdk/6.0*',
		"shared/Microsoft.AspNetCore.App$osAppExt/6.0*",
		"shared/Microsoft.NETCore.App$osAppExt/6.0*"
	)
}


foreach ($rmPath in $rmPaths)
{
	Remove-Item -Recurse -Force -Path (Join-Path -Path $DotnetRoot -ChildPath $rmPath)
}

Remove-Item -Recurse -Force -Path ~/.templateengine
