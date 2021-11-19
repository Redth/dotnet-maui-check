
# dotnet-maui-check
.NET MAUI Check tool

![.NET MAUI Check](https://user-images.githubusercontent.com/271950/112761851-29f53180-8fcb-11eb-92be-c843c794b2af.gif)

To install:
```
dotnet tool install -g Redth.Net.Maui.Check
```

To run:
```
maui-check
```

## Troubleshooting

If you run into problems with maui-check, you should generally try the following:

1. Update the tool to the latest version: `dotnet tool update -g redth.net.maui.check --add-source https://api.nuget.org/v3/index.json`
2. Run with `maui-check --force-dotnet` to ensure the workload repair/update/install commands run regardless of if maui-check thinks the workload versions look good
3. If you have errors still, it may help to run the [Clean-Old-DotNet6-Previews.ps1](https://github.com/Redth/dotnet-maui-check/blob/main/Clean-Old-DotNet6-Previews.ps1) script to remove old SDK Packs, templates, or otherwise old cached preview files that might be causing the problem.  Try running `maui-check --force-dotnet` again after this step.
4. Finally, if you have problems, run with `--verbose` flag and capture the output and add it to a new issue.

## Command line arguments

### `-m <FILE_OR_URL>`, `--manifest <FILE_OR_URL>` Manifest File or Url

Manifest files are currently used by the doctor to fetch the latest versions and requirements.
The manifest is hosted by default at: https://aka.ms/dotnet-maui-check-manifest
Use this option to specify an alternative file path or URL to use.

```
maui-check --manifest /some/other/file
```

### `-f`, `--fix` Fix without prompt

You can try using the `--fix` argument to automatically enable solutions to run without being prompted.

```
maui-check --fix
```

### `-n`, `--non-interactive` Non-Interactive

If you're running on CI you may want to run without any required input with the `--non-interactive` argument.  You can combine this with `--fix` to automatically fix without prompting.

```
maui-check --non-interactive
```

### `--preview` Preview Manifest feed

This uses a more frequently updated manifest with newer versions of things more often.
The manifest is hosted by default at: https://aka.ms/dotnet-maui-check-manifest-dev

```
maui-check --preview
```

### `--ci` Continuous Integration

Uses the dotnet-install powershell / bash scripts for installing the dotnet SDK version from the manifest instead of the global installer.

```
maui-check --ci
```


### `-s <ID_OR_NAME>`, `--skip <ID_OR_NAME>` Skip Checkup

Skips a checkup by name or id as listed in `maui-check list`.
NOTE: If there are any other checkups which depend on a skipped checkup, they will be skipped too. 

```
maui-check --skip openjdk --skip androidsdk
```

### `list` List Checkups

Lists possible checkups in the format: `checkup_id (checkup_name)`.
These can be used to specify `--skip checkup_id`, `-s checkup_name` arguments.


### `config` Configure global.json and NuGet.config in Working Dir

This allows you to quickly synchronize your `global.json` and/or `NuGet.config` in the current working directory to utilize the values specified in the manifest.

Arguments:
 - `--dotnet` or `--dotnet-version`: Use the SDK version in the manifest in `global.json`.
 - `--dotnet-pre true|false`: Change the `allowPrerelease` value in the `global.json`.
 - `--dotnet-rollForward <OPTION>`: Change the `rollForward` value in `global.json` to one of the allowed values specified.
 - `--nuget` or `--nuget-sources`: Adds the nuget sources specified in the manifest to the `NuGet.config` and creates the file if needed.

Example:

`maui-check config --dev --nuget-sources --dotnet-version --dotnet-pre true`

