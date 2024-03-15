namespace UglyToad.PdfPig.Tests.Writer
{
    using Integration;
    using PdfPig.Content;
    using PdfPig.Writer;
    using System.Xml.Linq;

    public class XmpTests
    {
        const string RdfNamespace = "http://www.w3.org/1999/02/22-rdf-syntax-ns#";
        const string PdfaidNamespace = "http://www.aiim.org/pdfa/ns/id/";
        const string FtxFormsNamespace = "http://ns.ftx.com/forms/1.0/";
        const string FtxControldataNamespace = "http://ns.ftx.com/forms/1.0/controldata/";

        [Fact]
        public void XmpInfoIsWrittenToPdfADocument()
        {
            byte[] pdfA2aDocument = BuildPdfA2aDocument();

            using (PdfDocument newDocument = PdfDocument.Open(pdfA2aDocument))
            {
                Assert.True(newDocument.TryGetXmpMetadata(out XmpMetadata xmpMetadata));
                XDocument xmp = xmpMetadata.GetXDocument();
                /* Should contain the the PDF/A-2a XMP nodes.
                 * <pdfaid:part>2</pdfaid:part>
                 * <pdfaid:conformance>A</pdfaid:conformance>
                 */
                Assert.Equal("2", xmp.Descendants(XNamespace.Get(PdfaidNamespace) + "part").First().Value);
                Assert.Equal("A", xmp.Descendants(XNamespace.Get(PdfaidNamespace) + "conformance").First().Value);
            }
        }

        [Fact]
        public void CustomXmpInfoIsMergedIntoPdfADocumentXmp()
        {
            using (PdfDocument simpleDocument = PdfDocument.Open(BuildPdfA2aDocument()))
            {
                PdfDocumentBuilder pdfDocumentBuilder = new()
                {
                    ArchiveStandard = PdfAStandard.A2A,
                    IncludeDocumentInformation = true,
                    XmpMetadata = XDocument.Parse(@"<x:xmpmeta xmlns:x=""adobe:ns:meta/"" x:xmptk=""Adobe XMP Core 5.6-c014 79.156797, 2014/08/20-09:53:02        "">
                          <rdf:RDF xmlns:rdf=""http://www.w3.org/1999/02/22-rdf-syntax-ns#"">
                            <rdf:Description
	                         xmlns:ftx=""http://ns.ftx.com/forms/1.0/""
	                         xmlns:control=""http://ns.ftx.com/forms/1.0/controldata/""
	                         xmlns:pdfaid=""http://www.aiim.org/pdfa/ns/id/""
	                         xmlns:pdf=""http://ns.adobe.com/pdf/1.3/""
	                         >
                              <ftx:ControlData rdf:parseType=""Resource"">
                                <control:Anzahl_Zeichen_Titel>0</control:Anzahl_Zeichen_Titel>
                                <control:Anzahl_Zeichen_Vorname>0</control:Anzahl_Zeichen_Vorname>
                                <control:Anzahl_Zeichen_Namenszusatz>0</control:Anzahl_Zeichen_Namenszusatz>
                                <control:Anzahl_Zeichen_Hausnummer>0</control:Anzahl_Zeichen_Hausnummer>
                                <control:Anzahl_Zeichen_Postleitzahl>0</control:Anzahl_Zeichen_Postleitzahl>
                                <control:Anzahl_Zeichen_Wohnsitzlaendercode>0</control:Anzahl_Zeichen_Wohnsitzlaendercode>
                                <control:Auftragsnummer_Einsender>0</control:Auftragsnummer_Einsender>
                                <control:Formularnummer>10</control:Formularnummer>
                                <control:Formularversion>10.2020</control:Formularversion>
                                <control:Technische_Version>6</control:Technische_Version>
                              </ftx:ControlData>
                              <pdfaid:part>1</pdfaid:part>
                              <pdfaid:conformance>B</pdfaid:conformance>
                            </rdf:Description>
                          </rdf:RDF>
                        </x:xmpmeta>"),
                };
                pdfDocumentBuilder.AddPage(simpleDocument, 1);

                using (PdfDocument xmpDocument = PdfDocument.Open(pdfDocumentBuilder.Build()))
                {
                    Assert.True(xmpDocument.TryGetXmpMetadata(out XmpMetadata xmpMetadata));
                    XDocument xmp = xmpMetadata.GetXDocument();
                    /* Should still contain exact one each of the correct PDF/A-2a XMP nodes.
                     * PDF/A-1b from the added XMP document must not be there.
                     * <pdfaid:part>2</pdfaid:part>
                     * <pdfaid:conformance>A</pdfaid:conformance>
                     */
                    Assert.Equal("2", xmp.Descendants(XNamespace.Get(PdfaidNamespace) + "part").First().Value);
                    Assert.Single(xmp.Descendants(XNamespace.Get(PdfaidNamespace) + "part"));
                    Assert.Equal("A", xmp.Descendants(XNamespace.Get(PdfaidNamespace) + "conformance").First().Value);
                    Assert.Single(xmp.Descendants(XNamespace.Get(PdfaidNamespace) + "conformance"));

                    // Should also contain the nodes from the added XMP document
                    Assert.Single(xmp.Descendants(XNamespace.Get(FtxFormsNamespace) + "ControlData"));
                    Assert.Equal("0", xmp.Descendants(XNamespace.Get(FtxControldataNamespace) + "Anzahl_Zeichen_Titel").First().Value);
                }
            }
        }

        private byte[] BuildPdfA2aDocument()
        {
            string simpleDoc = IntegrationHelpers.GetDocumentPath("Single Page Simple - from inkscape.pdf");
            using (PdfDocument pdfPigDocument = PdfDocument.Open(simpleDoc))
            {
                PdfDocumentBuilder pdfDocumentBuilder = new()
                {
                    ArchiveStandard = PdfAStandard.A2A,
                };
                pdfDocumentBuilder.AddPage(pdfPigDocument, 1);

                return pdfDocumentBuilder.Build();
            }
        }
    }
}
