using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using UglyToad.PdfPig.Tokens;

namespace UglyToad.PdfPig.Writer.Xmp
{
    internal static class XmpWriter
    {
        private const string Xmptk = "Adobe XMP Core 5.6-c014 79.156797, 2014/08/20-09:53:02        ";
        private const string RdfNamespace = "http://www.w3.org/1999/02/22-rdf-syntax-ns#";

        private const string XmpMetaPrefix = "x";
        private const string XmpMetaNamespace = "adobe:ns:meta/";


        private const string DublinCorePrefix = "dc";
        private const string DublinCoreNamespace = "http://purl.org/dc/elements/1.1/";

        private const string XmpBasicPrefix = "xmp";
        private const string XmpBasicNamespace = "http://ns.adobe.com/xap/1.0/";

        // ReSharper disable UnusedMember.Local
        private const string XmpRightsManagementPrefix = "xmpRights";
        private const string XmpRightsManagementNamespace = "http://ns.adobe.com/xap/1.0/rights/";

        private const string XmpMediaManagementPrefix = "xmpMM";
        private const string XmpMediaManagementNamespace = "http://ns.adobe.com/xap/1.0/mm/";
        // ReSharper restore UnusedMember.Local

        private const string AdobePdfPrefix = "pdf";
        private const string AdobePdfNamespace = "http://ns.adobe.com/pdf/1.3/";

        private const string PdfAIdentificationExtensionPrefix = "pdfaid";
        private const string PdfAIdentificationExtensionNamespace = "http://www.aiim.org/pdfa/ns/id/";
        
        public static StreamToken GenerateXmpStream(PdfDocumentBuilder.DocumentInformationBuilder builder, decimal version,
            PdfAStandard standard)
        {
            XNamespace xmpMeta = XmpMetaNamespace;
            XNamespace rdf = RdfNamespace;

            var emptyRdfAbout = new XAttribute(rdf + "about", string.Empty);

            var rdfDescriptionElement = new XElement(rdf + "Description", emptyRdfAbout);

            // Dublin Core Schema
            AddElementsForSchema(rdfDescriptionElement, DublinCorePrefix, DublinCoreNamespace, builder,
                new List<SchemaMapper>
                {
                    new SchemaMapper("format", b => "application/pdf"),
                    new SchemaMapper("creator", b => b.Author),
                    new SchemaMapper("description", b => b.Subject),
                    new SchemaMapper("title", b => b.Title)
                });

            // XMP Basic Schema
            AddElementsForSchema(rdfDescriptionElement, XmpBasicPrefix, XmpBasicNamespace, builder,
                new List<SchemaMapper>
                {
                    new SchemaMapper("CreatorTool", b => b.Creator)
                });

            // Adobe PDF Schema
            AddElementsForSchema(rdfDescriptionElement, AdobePdfPrefix, AdobePdfNamespace, builder,
                new List<SchemaMapper>
                {
                    new SchemaMapper("PDFVersion", b => "1.7"),
                    new SchemaMapper("Producer", b => b.Producer)
                });

            var pdfAIdContainer = GetVersionAndConformanceLevelIdentificationElement(rdf, emptyRdfAbout, standard);
            
            var document = new XDocument(
                new XElement(xmpMeta + "xmpmeta", GetNamespaceAttribute(XmpMetaPrefix, XmpMetaNamespace),
                    new XAttribute(xmpMeta + "xmptk", Xmptk),
                    new XElement(rdf + "RDF",
                        GetNamespaceAttribute("rdf", rdf),
                        rdfDescriptionElement,
                        pdfAIdContainer
                    )
                )
            );

            var xml = document.ToString(SaveOptions.None).Replace("\r\n", "\n");
            xml = $"<?xpacket begin=\"\ufeff\" id=\"W5M0MpCehiHzreSzNTczkc9d\"?>\n{xml}\n<?xpacket end=\"r\"?>";

            var bytes = Encoding.UTF8.GetBytes(xml);

            return new StreamToken(new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                {NameToken.Type, NameToken.Metadata},
                {NameToken.Subtype, NameToken.Xml},
                {NameToken.Length, new NumericToken(bytes.Length)}
            }), bytes);
        }

        private static XAttribute GetNamespaceAttribute(string prefix, XNamespace ns) => new XAttribute(XNamespace.Xmlns + prefix, ns);

        private static void AddElementsForSchema(XElement parent, string prefix, string ns, PdfDocumentBuilder.DocumentInformationBuilder builder,
            List<SchemaMapper> mappers)
        {
            var xns = XNamespace.Get(ns);
            parent.Add(GetNamespaceAttribute(prefix, xns));

            foreach (var mapper in mappers)
            {
                var value = mapper.ValueFunc(builder);

                if (value == null)
                {
                    continue;
                }

                parent.Add(new XElement(xns + mapper.Name, value));
            }
        }

        private static XElement GetVersionAndConformanceLevelIdentificationElement(XNamespace rdf, XAttribute emptyRdfAbout, PdfAStandard standard)
        {
            /*
             * The only mandatory XMP entries are those which indicate that the file is a PDF/A-1 file and its conformance level. 
             * The PDF/A version and conformance level of a file shall be specified using the PDF/A Identification extension schema. 
             */
            XNamespace pdfaid = PdfAIdentificationExtensionNamespace;
            var pdfAidContainer = new XElement(rdf + "Description", emptyRdfAbout, GetNamespaceAttribute(PdfAIdentificationExtensionPrefix, pdfaid));

            int part;
            string conformance;
            switch (standard)
            {
                case PdfAStandard.A1B:
                    part = 1;
                    conformance = "B";
                    break;
                case PdfAStandard.A1A:
                    part = 1;
                    conformance = "A";
                    break;
                case PdfAStandard.A2B:
                    part = 2;
                    conformance = "B";
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(standard), standard, null);
            }

            pdfAidContainer.Add(new XElement(pdfaid + "part", part));
            pdfAidContainer.Add(new XElement(pdfaid + "conformance", conformance));

            return pdfAidContainer;
        }

        // Potentially required for further PDF/A versions.
        // ReSharper disable once UnusedMember.Local
        private static XElement GetExtensionSchemasElement(XNamespace rdf, XAttribute emptyRdfAbout)
        {
            const string pdfAExtensionSchemaContainerSchemaPrefix = "pdfaExtension";
            const string pdfAExtensionSchemaContainerSchemaUri = "http://www.aiim.org/pdfa/ns/extension/";
            const string pdfASchemaValueTypePrefix = "pdfaSchema";
            const string pdfASchemaValueTypeUri = "http://www.aiim.org/pdfa/ns/schema#";
            const string pdfAPropertyValueTypePrefix = "pdfaProperty";
            const string pdfAPropertyValueTypeUri = "http://www.aiim.org/pdfa/ns/property#";

            XNamespace pdfaExtension = pdfAExtensionSchemaContainerSchemaUri;
            XNamespace pdfaSchema = pdfASchemaValueTypeUri;
            XNamespace pdfaProperty = pdfAPropertyValueTypeUri;

            var pdfaSchemaContainer = new XElement(rdf + "Description", emptyRdfAbout, 
                GetNamespaceAttribute(pdfAExtensionSchemaContainerSchemaPrefix, pdfaExtension),
                GetNamespaceAttribute(pdfASchemaValueTypePrefix, pdfaSchema),
                GetNamespaceAttribute(pdfAPropertyValueTypePrefix, pdfaProperty));

            var schemaBag = new XElement(pdfaExtension + "schemas",
                new XElement(rdf + "Bag"));

            var individualSchemaContainer = new XElement(rdf + "li", new XAttribute(rdf + "parseType", "Resource"));

            individualSchemaContainer.Add(new XElement(pdfaSchema + "namespaceURI", PdfAIdentificationExtensionNamespace));
            individualSchemaContainer.Add(new XElement(pdfaSchema + "prefix", PdfAIdentificationExtensionPrefix));
            individualSchemaContainer.Add(new XElement(pdfaSchema + "schema", "PDF/A ID Schema"));

            var seqContainer = new XElement(pdfaSchema + "property", new XElement(rdf + "Seq"));

            var seq = seqContainer.Elements().Last();

            seq.Add(GetSchemaPropertyListItem(rdf, pdfaProperty, "part", "Part of PDF/A standard", "internal", "Integer"));
            seq.Add(GetSchemaPropertyListItem(rdf, pdfaProperty, "amd", "Amendment of PDF/A standard"));
            seq.Add(GetSchemaPropertyListItem(rdf, pdfaProperty, "conformance", "Conformance level of PDF/A standard"));

            individualSchemaContainer.Add(seqContainer);

            schemaBag.Elements().Last().Add(individualSchemaContainer);

            pdfaSchemaContainer.Add(schemaBag);

            return pdfaSchemaContainer;
        }

        private static XElement GetSchemaPropertyListItem(XNamespace rdfNs,
            XNamespace pdfaPropertyNs, string name, string description, string category = "internal", string valueType = "Text")
        {
            var li = new XElement(rdfNs + "li", new XAttribute(rdfNs + "parseType", "Resource"));

            li.Add(new XElement(pdfaPropertyNs + "category", category));
            li.Add(new XElement(pdfaPropertyNs + "description", description));
            li.Add(new XElement(pdfaPropertyNs + "name", name));
            li.Add(new XElement(pdfaPropertyNs + "valueType", valueType));

            return li;
        }

        private class SchemaMapper
        {
            public string Name { get; }

            public Func<PdfDocumentBuilder.DocumentInformationBuilder, string> ValueFunc { get; }

            public SchemaMapper(string name, Func<PdfDocumentBuilder.DocumentInformationBuilder, string> valueFunc)
            {
                Name = name;
                ValueFunc = valueFunc;
            }
        }
    }
}
