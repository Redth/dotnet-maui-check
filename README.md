# dotnet-maui-doctor
.NET MAUI Doctor tool

![.NET MAUI Doctor](https://user-images.githubusercontent.com/271950/110705286-305e6d80-81c4-11eb-993f-0d2d772b2260.gif)


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

