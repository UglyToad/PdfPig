# Contributing to PdfPig #

Welcome and thanks for your interest in PdfPig.

We're always looking for contributions which improve the stability of PdfPig and add missing features.

There are many ways you can get involved, from raising issues, writing or improving documentation and writing code.

## Getting Started ##

PdfPig is a C# .NET project. For this reason development is easiest on Windows.

To get started simply clone the code, you will need Git on your system to do this. [Git for Windows](https://gitforwindows.org/) can be downloaded and installed easily. Once you have Git on your system you can get the code by cloning:

```git clone https://github.com/UglyToad/PdfPig.git```

### Windows ###

Once you have the code you can build it using Visual Studio. Visual Studio comes with a community edition which is free to use - https://visualstudio.microsoft.com/vs/community/. Because PdfPig support multiple versions of the .NET framework you will need multiple versions installed to build it, alternatively see the other "Linux and Mac" section:

+ Microsoft .NET Framework 4.5 SDK
+ Microsoft .NET Framework 4.6 SDK
+ Microsoft .NET Framework 4.6.1 SDK
+ Microsoft .NET Framework 4.7 SDK
+ Microsoft .NET Core SDK >= 2.1.0

### Linux and Mac ###

*The guide below is untested*.

Because one of the Target Frameworks for PdfPig is .NET Standard 2.0 it may be possible to build and develop it on non-Windows systems.

First you will need a .NET Core development environment set up. You will need the .NET Core 2.0 SDK.

+ For Linux - https://docs.microsoft.com/en-us/dotnet/core/linux-prerequisites?tabs=netcore2x
+ For Mac - https://docs.microsoft.com/en-us/dotnet/core/macos-prerequisites?tabs=netcore2x

Once you have a `dotnet` environment set up you should be able to run `dotnet --version` from a terminal to see the installed version.

#### Before Building ####

The project supports some target frameworks which aren't supported on Linux or Mac. You can remove these from the Project files to support building on non-Windows systems.

Open `.\src\UglyToad.PdfPig\UglyToad.PdfPig.csproj` in a text editor and change:

```
  <PropertyGroup>
    <TargetFrameworks>netstandard2.0;net45;net451;net452;net46;net461;net462;net47;net6.0</TargetFrameworks>
    <PackageId>PdfPig</PackageId>
    ...
```

To:

```
  <PropertyGroup>
    <TargetFrameworks>netstandard2.0</TargetFrameworks>
    <PackageId>PdfPig</PackageId>
```
**Please do not commit this change when submitting a PR**.

#### Build ####

With a terminal inside the `src` folder you can run `dotnet restore` followed by `dotnet build` to build the project.

#### Development Environment ####

You can use the tool of your choice to develop on non-Windows environments (or even on Windows!) by invoking the dotnet build tools from the command line.

For intellisense and refactoring support you can use Visual Studio for Mac on Mac systems and Visual Studio Code on all systems. See this guide for Visual Studio Code: https://code.visualstudio.com/docs/languages/dotnet.

## Developing ##

The PdfPig code is split into 2 logical sections, reading and writing PDF files. The entry point for [all reading related activities is](https://github.com/UglyToad/PdfPig/blob/master/src/UglyToad.PdfPig/PdfDocument.cs):

```.\src\UglyToad.PdfPig\PdfDocument.cs```

All calls from here are passed through to the `PdfDocumentFactory` which serves as the composition root for parsing a PDF file.

A bird's eye view of the process of parsing is:

1. Parse the file header to make sure we're dealing with a PDF file.
2. Find the cross-reference (xref) table or stream at the end of the document which indicates the location of all numbered PDF objects in the file.
3. Parse the trailer dictionary which contains the location of the pages tree, information dictionary, any encryption material, etc.
4. Return a new `PdfDocument` containing the objects and information necessary to load and parse all other parts of the document.

Consult the full PDF specification for further information, I use [the Sixth Edition](https://www.adobe.com/content/dam/acom/en/devnet/pdf/pdf_reference_archive/pdf_reference_1-7.pdf) but there are newer specifications [available](https://www.adobe.com/content/dam/acom/en/devnet/pdf/pdfs/PDF32000_2008.pdf). PDF 2.0 is defined in an ISO document which is unfortunately not available for free so PDF 2.0 features are not implemented in general.

### Architecture and Style ###

In general the aim is for objects to be immutable and avoid state as much as possible. Prefer `IReadOnlyList<T>` and `get` only properties where possible. As far as possible services should not use state and should instead take data as method parameters.

The code base is still evolving towards an established design, meanwhile consult the existing code for examples of style, naming, etc.

## Issues ##

Issue reports help us improve the software for all users and are very valuable. However it is extremely difficult to assist people if issue reports lack some key information. Please provide as far as possible:

+ Documents that reproduce the issue being described.
+ Full stack traces and inner exception details for any exceptions being encountered.
+ Details on the system (OS, Framework Version, etc.) any errors are being encountered on.

It is most important to provide the document. If this is not possible due to data protection then we'll try our best to help, but it's unlikely we'll be able to fix the issue.

In addition issues can serve as a way to request features and ask questions about usage of the library.

Please remember no one is getting paid for this and try to be understanding if we can't help you.