
# dotnet-maui-doctor
.NET MAUI Doctor tool

![.NET MAUI Doctor](https://user-images.githubusercontent.com/271950/111553662-3c65a480-875b-11eb-9e67-3738d3f7e0ad.gif)


To run:
```
maui-doctor
```

To run and try and fix issues:
```
maui-doctor --fix
```


## Manifest Files

Manifest files are currently used by the doctor to fetch the latest versions and requirements.

The manifest is hosted by default at: https://aka.ms/dotnet-maui-doctor-manifest

You can specify an alternative file or URL with the `--manifest <URL>` option:

```
maui-doctor --manifest /some/other/file
```

