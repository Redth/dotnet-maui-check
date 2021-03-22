
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


## Manifest Files

Manifest files are currently used by the doctor to fetch the latest versions and requirements.

The manifest is hosted by default at: https://aka.ms/dotnet-maui-check-manifest

You can specify an alternative file or URL with the `--manifest <URL>` option:

```
maui-check --manifest /some/other/file
```

## Fix silently

You can try using the `--fix` argument to automatically enable solutions to run without being prompted.

