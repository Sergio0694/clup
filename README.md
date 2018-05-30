# clup

[![NuGet](https://img.shields.io/nuget/v/clup.svg)](https://www.nuget.org/packages/clup/) [![NuGet](https://img.shields.io/nuget/dt/clup.svg)](https://www.nuget.org/stats/packages/clup?groupby=Version) [![Twitter Follow](https://img.shields.io/twitter/follow/Sergio0694.svg?style=flat&label=Follow)](https://twitter.com/SergioPedri)

A .NET Core 2.1 CLI tool to easily remove duplicate files.

## Installing from DotGet

Make sure to get the [.NET Core 2.1 Runtime](https://www.microsoft.com/net/download/dotnet-core/runtime-2.1.0), then just run this command:

```
dotnet tool install clup -g
```

And that's it, you're ready to go!

## Quick start

**clup** has three main commands: `delete`, `move` and `list`. 

While they share most of the available options, the main difference is that `delete` automatically removes all the duplicate files it founds (leaving only the original files), `move` keeps the duplicate files after moving them in a specified directory, and `list` just writes down a summary of the discovered duplicate files.

Other options include:
* `minsize` and `maxsize`: used to specify a min/max size (in bytes) for the files to be processed and deleted
* `mode`: to indicate whether to just use the MD5 hash of the files contents to check for duplicates, or to also include the files extensions or complete filenames.
* `include`: a list of file extensions to use to filter the files in the source directory
* `exclude`: an optional list of file extensions to ignore (this option and `include` are mutually exclusive)

## Dependencies

The libraries use the following libraries and NuGet packages:

* [CommandLineParser](https://www.nuget.org/packages/commandlineparser/)
* [Ben.Demystifier](https://www.nuget.org/packages/Ben.Demystifier/)
* [JetBrains.Annotations](https://www.nuget.org/packages/JetBrains.Annotations/)
