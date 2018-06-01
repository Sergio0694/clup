# clup - clean duplicates

[![NuGet](https://img.shields.io/nuget/v/clup.svg)](https://www.nuget.org/packages/clup/) [![NuGet](https://img.shields.io/nuget/dt/clup.svg)](https://www.nuget.org/stats/packages/clup?groupby=Version) [![Twitter Follow](https://img.shields.io/twitter/follow/Sergio0694.svg?style=flat&label=Follow)](https://twitter.com/SergioPedri)

A .NET Core 2.1 CLI tool to easily remove duplicate files.

## Installing from DotGet

Make sure to get the [.NET Core 2.1 SDK](https://www.microsoft.com/net/download/dotnet-core/sdk-2.1.300), then just run this command:

```
dotnet tool install clup -g
```

And that's it, you're ready to go!

## Quick start

**clup** has three main commands: `delete`, `move` and `list`. 

While they share most of the available options, the main difference is that `delete` automatically removes all the duplicate files it founds (leaving only the original files), `move` keeps the duplicate files after moving them in a specified directory, and `list` just writes down a summary of the discovered duplicate files.

Other options include:
* `-m` | `--minsize` and `-M` | `--maxsize`: used to specify a min/max size (in bytes) for the files to be processed and deleted.
* `-h` | `--hash`: to indicate whether to just use the MD5 hash of the files contents to check for duplicates, or to also include the files extensions or complete filenames.
* `-i` | `--include`: a list of file extensions to use to filter the files in the source directory.
* `-e` | `--exclude`: an optional list of file extensions to ignore (this option and `include` are mutually exclusive).
* `-b` | `--beep`: play a short feedback sound when the requested operation completes.
* `-v` | `--verbose`: display additional info after analyzing the source directory.
* `--source-current`: use the current working directory as the source path.

### Examples

Find and remove duplicate files from the specified path, notify when the operation finishes and play a notification sound:

```
clup remove -s c:\users\myname\downloads -v -b
```
Find duplicate files from the current directory and save a detailed log:

```
clup list --source-current --logdir-root -v
```

## Dependencies

The libraries use the following libraries and NuGet packages:

* [CommandLineParser](https://www.nuget.org/packages/commandlineparser/)
* [JetBrains.Annotations](https://www.nuget.org/packages/JetBrains.Annotations/)
