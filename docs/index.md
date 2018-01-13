<image src="https://raw.githubusercontent.com/UglyToad/Pdf/master/documentation/pdfpig.png" width="128px" height="128px"/>

PdfPig is a fully open-source .NET standard compatible library that enables users to extract text content from PDFs in C#.

Using PdfPig users can read text from a PDF in C# without the need for commercial libraries.

## Credit ##

This project wouldn't be possible without the work done by the [PDFBox](https://pdfbox.apache.org/) team and the Apache Foundation.

## Usage ##

The ```PdfDocument``` class provides access to the contents of a document loaded either from file or passed in as bytes. To open from a file use the ```PdfDocument.Open``` static method:

    using UglyToad.PdfPig;
    using UglyToad.PdfPig.Content;

    using (PdfDocument document = PdfDocument.Open(@"C:\my-file.pdf"))
    {
        int pageCount = document.NumberOfPages;

        Page page = document.GetPage(1);

        decimal widthInPoints = page.Width;
        decimal heightInPoints = page.Height;

        string text = page.Text;
    }
    
```PdfDocument``` should only be used in a ```using``` statement since it implements ```IDisposable``` (unless the consumer disposes of it elsewhere).

Since this is alpha software the consumer should wrap all access in a ```try catch``` block since it is extremely likely to throw exceptions. As a fallback you can try running PDFBox using [IKVM](https://www.ikvm.net/) or using [PDFsharp](http://www.pdfsharp.net).

The document contains the version of the PDF specification it complies with, accessed by ```document.Version```:

    decimal version = document.Version;

### Document Information ###

The ```PdfDocument``` provides access to the document metadata as ```DocumentInformation``` defined in the PDF file. These tend not to be provided therefore most of these entries will be ```null```:

    PdfDocument document = PdfDocument.Open(fileName);

    // The name of the program used to convert this document to PDF.
    string producer = document.Information.Producer;

    // The title given to the document
    string title = document.Information.Title;
    // etc...

### Page ###
    
The ```Page``` contains the page width and height in points as well as mapping to the ```PageSize``` enum:

    PageSize size = Page.Size;
    
    bool isA4 = size == PageSize.A4;

```Page``` provides access to the text of the page:

    string text = page.Text;

### Letter ###

Due to the way a PDF is structured internally the page text may not be a readable representation of the text as it appears in the document. Since PDF is a presentation format, text can be drawn in any order, not necessarily reading order. This means spaces may be missing or words may be in unexpected positions in the text.

To help users resolve actual text order on the page, the ```Page``` file provides access to a list of the letters:


    IReadOnlyList<Letter> letters = page.Letters;

These letters contain:

+ The text of the letter: ```letter.Value```.
+ The location of the lower left of the letter: ```letter.Location```.
+ The width of the letter: ```letter.Width```.
+ The font size in unscaled relative text units (these sizes are internal to the PDF and do not correspond to sizes in pixels, points or other units): ```letter.FontSize```.
+ The name of the font used to render the letter if available: ```letter.FontName```.

Letter position is measured in PDF coordinates where the origin is the lower left corner of the page. Therefore an higher Y value means closer to the top of the page.

At this stage letter position is experimental and **will change in future versions**! Do not rely on letter positions remaining constant between different versions of this package.

## Installation ##

The **pre-release** package is available via the releases tab or from Nuget:

https://www.nuget.org/packages/PdfPig/