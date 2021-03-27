
# dotnet-maui-check
.NET MAUI Check tool

![.NET MAUI Check](https://user-images.githubusercontent.com/271950/111553662-3c65a480-875b-11eb-9e67-3738d3f7e0ad.gif)


To install:
```
dotnet tool install -g Redth.Net.Maui.Check
```

To run:
```
maui-check
```

# Command line arguments

## `-m <FILE_OR_URL>`, `--manifest <FILE_OR_URL>` Manifest File or Url

Manifest files are currently used by the doctor to fetch the latest versions and requirements.
The manifest is hosted by default at: https://aka.ms/dotnet-maui-check-manifest
Use this option to specify an alternative file path or URL to use.

```
maui-check --manifest /some/other/file
```

## `-f`, `--fix` Fix without prompt

You can try using the `--fix` argument to automatically enable solutions to run without being prompted.

```
maui-check --fix
```

## `-n`, `--non-interactive` Non-Interactive

If you're running on CI you may want to run without any required input with the `--non-interactive` argument.  You can combine this with `--fix` to automatically fix without prompting.

```
maui-check --non-interactive
```

## `-d`, `--dev` Dev Manifest feed

This uses a more frequently updated manifest with newer versions of things more often.
The manifest is hosted by default at: https://aka.ms/dotnet-maui-check-manifest-dev

```
maui-check --dev
```

## `-s <ID_OR_NAME>`, `--skip <ID_OR_NAME>` Skip Checkup

Skips a checkup by name or id as listed in `maui-check list`.
NOTE: If there are any other checkups which depend on a skipped checkup, they will be skipped too. 

```
maui-check --skip openjdk --skip androidsdk
```

## `list` List Checkups

Lists possible checkups in the format: `checkup_id (checkup_name)`.
These can be used to specify `--skip checkup_id`, `-s checkup_name` arguments.
