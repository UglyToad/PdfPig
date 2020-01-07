namespace UglyToad.PdfPig.DocumentLayoutAnalysis.Export.PAGE
{
    using System;
    using System.CodeDom.Compiler;
    using System.ComponentModel;
    using System.Xml.Serialization;

    public partial class PageXmlDocument
    {
        /// <summary>
        /// iso15924 2016-07-14
        /// </summary>
        [EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [GeneratedCode("xsd", "4.6.1055.0")]
        [Serializable()]
        [XmlType(Namespace = "http://schema.primaresearch.org/PAGE/gts/pagecontent/2019-07-15")]
        public enum PageXmlScriptSimpleType
        {

            /// <remarks/>
            [XmlEnum("Adlm - Adlam")]
            AdlmAdlam,

            /// <remarks/>
            [XmlEnum("Afak - Afaka")]
            AfakAfaka,

            /// <remarks/>
            [XmlEnum("Aghb - Caucasian Albanian")]
            AghbCaucasianAlbanian,

            /// <remarks/>
            [XmlEnum("Ahom - Ahom, Tai Ahom")]
            AhomAhomTaiAhom,

            /// <remarks/>
            [XmlEnum("Arab - Arabic")]
            ArabArabic,

            /// <remarks/>
            [XmlEnum("Aran - Arabic (Nastaliq variant)")]
            AranArabicNastaliqVariant,

            /// <remarks/>
            [XmlEnum("Armi - Imperial Aramaic")]
            ArmiImperialAramaic,

            /// <remarks/>
            [XmlEnum("Armn - Armenian")]
            ArmnArmenian,

            /// <remarks/>
            [XmlEnum("Avst - Avestan")]
            AvstAvestan,

            /// <remarks/>
            [XmlEnum("Bali - Balinese")]
            BaliBalinese,

            /// <remarks/>
            [XmlEnum("Bamu - Bamum")]
            BamuBamum,

            /// <remarks/>
            [XmlEnum("Bass - Bassa Vah")]
            BassBassaVah,

            /// <remarks/>
            [XmlEnum("Batk - Batak")]
            BatkBatak,

            /// <remarks/>
            [XmlEnum("Beng - Bengali")]
            BengBengali,

            /// <remarks/>
            [XmlEnum("Bhks - Bhaiksuki")]
            BhksBhaiksuki,

            /// <remarks/>
            [XmlEnum("Blis - Blissymbols")]
            BlisBlissymbols,

            /// <remarks/>
            [XmlEnum("Bopo - Bopomofo")]
            BopoBopomofo,

            /// <remarks/>
            [XmlEnum("Brah - Brahmi")]
            BrahBrahmi,

            /// <remarks/>
            [XmlEnum("Brai - Braille")]
            BraiBraille,

            /// <remarks/>
            [XmlEnum("Bugi - Buginese")]
            BugiBuginese,

            /// <remarks/>
            [XmlEnum("Buhd - Buhid")]
            BuhdBuhid,

            /// <remarks/>
            [XmlEnum("Cakm - Chakma")]
            CakmChakma,

            /// <remarks/>
            [XmlEnum("Cans - Unified Canadian Aboriginal Syllabics")]
            CansUnifiedCanadianAboriginalSyllabics,

            /// <remarks/>
            [XmlEnum("Cari - Carian")]
            CariCarian,

            /// <remarks/>
            [XmlEnum("Cham - Cham")]
            ChamCham,

            /// <remarks/>
            [XmlEnum("Cher - Cherokee")]
            CherCherokee,

            /// <remarks/>
            [XmlEnum("Cirt - Cirth")]
            CirtCirth,

            /// <remarks/>
            [XmlEnum("Copt - Coptic")]
            CoptCoptic,

            /// <remarks/>
            [XmlEnum("Cprt - Cypriot")]
            CprtCypriot,

            /// <remarks/>
            [XmlEnum("Cyrl - Cyrillic")]
            CyrlCyrillic,

            /// <remarks/>
            [XmlEnum("Cyrs - Cyrillic (Old Church Slavonic variant)")]
            CyrsCyrillicOldChurchSlavonicVariant,

            /// <remarks/>
            [XmlEnum("Deva - Devanagari (Nagari)")]
            DevaDevanagariNagari,

            /// <remarks/>
            [XmlEnum("Dsrt - Deseret (Mormon)")]
            DsrtDeseretMormon,

            /// <remarks/>
            [XmlEnum("Dupl - Duployan shorthand, Duployan stenography")]
            DuplDuployanShorthandDuployanStenography,

            /// <remarks/>
            [XmlEnum("Egyd - Egyptian demotic")]
            EgydEgyptianDemotic,

            /// <remarks/>
            [XmlEnum("Egyh - Egyptian hieratic")]
            EgyhEgyptianHieratic,

            /// <remarks/>
            [XmlEnum("Egyp - Egyptian hieroglyphs")]
            EgypEgyptianHieroglyphs,

            /// <remarks/>
            [XmlEnum("Elba - Elbasan")]
            ElbaElbasan,

            /// <remarks/>
            [XmlEnum("Ethi - Ethiopic")]
            EthiEthiopic,

            /// <remarks/>
            [XmlEnum("Geok - Khutsuri (Asomtavruli and Nuskhuri)")]
            GeokKhutsuriAsomtavruliAndNuskhuri,

            /// <remarks/>
            [XmlEnum("Geor - Georgian (Mkhedruli)")]
            GeorGeorgianMkhedruli,

            /// <remarks/>
            [XmlEnum("Glag - Glagolitic")]
            GlagGlagolitic,

            /// <remarks/>
            [XmlEnum("Goth - Gothic")]
            GothGothic,

            /// <remarks/>
            [XmlEnum("Gran - Grantha")]
            GranGrantha,

            /// <remarks/>
            [XmlEnum("Grek - Greek")]
            GrekGreek,

            /// <remarks/>
            [XmlEnum("Gujr - Gujarati")]
            GujrGujarati,

            /// <remarks/>
            [XmlEnum("Guru - Gurmukhi")]
            GuruGurmukhi,

            /// <remarks/>
            [XmlEnum("Hanb - Han with Bopomofo")]
            HanbHanwithBopomofo,

            /// <remarks/>
            [XmlEnum("Hang - Hangul")]
            HangHangul,

            /// <remarks/>
            [XmlEnum("Hani - Han (Hanzi, Kanji, Hanja)")]
            HaniHanHanziKanjiHanja,

            /// <remarks/>
            [XmlEnum("Hano - Hanunoo (Hanunóo)")]
            HanoHanunooHanunóo,

            /// <remarks/>
            [XmlEnum("Hans - Han (Simplified variant)")]
            HansHanSimplifiedVariant,

            /// <remarks/>
            [XmlEnum("Hant - Han (Traditional variant)")]
            HantHanTraditionalVariant,

            /// <remarks/>
            [XmlEnum("Hatr - Hatran")]
            HatrHatran,

            /// <remarks/>
            [XmlEnum("Hebr - Hebrew")]
            HebrHebrew,

            /// <remarks/>
            [XmlEnum("Hira - Hiragana")]
            HiraHiragana,

            /// <remarks/>
            [XmlEnum("Hluw - Anatolian Hieroglyphs")]
            HluwAnatolianHieroglyphs,

            /// <remarks/>
            [XmlEnum("Hmng - Pahawh Hmong")]
            HmngPahawhHmong,

            /// <remarks/>
            [XmlEnum("Hrkt - Japanese syllabaries")]
            HrktJapaneseSyllabaries,

            /// <remarks/>
            [XmlEnum("Hung - Old Hungarian (Hungarian Runic)")]
            HungOldHungarianHungarianRunic,

            /// <remarks/>
            [XmlEnum("Inds - Indus (Harappan)")]
            IndsIndusHarappan,

            /// <remarks/>
            [XmlEnum("Ital - Old Italic (Etruscan, Oscan etc.)")]
            ItalOldItalicEtruscanOscanEtc,

            /// <remarks/>
            [XmlEnum("Jamo - Jamo")]
            JamoJamo,

            /// <remarks/>
            [XmlEnum("Java - Javanese")]
            JavaJavanese,

            /// <remarks/>
            [XmlEnum("Jpan - Japanese")]
            JpanJapanese,

            /// <remarks/>
            [XmlEnum("Jurc - Jurchen")]
            JurcJurchen,

            /// <remarks/>
            [XmlEnum("Kali - Kayah Li")]
            KaliKayahLi,

            /// <remarks/>
            [XmlEnum("Kana - Katakana")]
            KanaKatakana,

            /// <remarks/>
            [XmlEnum("Khar - Kharoshthi")]
            KharKharoshthi,

            /// <remarks/>
            [XmlEnum("Khmr - Khmer")]
            KhmrKhmer,

            /// <remarks/>
            [XmlEnum("Khoj - Khojki")]
            KhojKhojki,

            /// <remarks/>
            [XmlEnum("Kitl - Khitan large script")]
            KitlKhitanlargescript,

            /// <remarks/>
            [XmlEnum("Kits - Khitan small script")]
            KitsKhitansmallscript,

            /// <remarks/>
            [XmlEnum("Knda - Kannada")]
            KndaKannada,

            /// <remarks/>
            [XmlEnum("Kore - Korean (alias for Hangul + Han)")]
            KoreKoreanaliasforHangulHan,

            /// <remarks/>
            [XmlEnum("Kpel - Kpelle")]
            KpelKpelle,

            /// <remarks/>
            [XmlEnum("Kthi - Kaithi")]
            KthiKaithi,

            /// <remarks/>
            [XmlEnum("Lana - Tai Tham (Lanna)")]
            LanaTaiThamLanna,

            /// <remarks/>
            [XmlEnum("Laoo - Lao")]
            LaooLao,

            /// <remarks/>
            [XmlEnum("Latf - Latin (Fraktur variant)")]
            LatfLatinFrakturvariant,

            /// <remarks/>
            [XmlEnum("Latg - Latin (Gaelic variant)")]
            LatgLatinGaelicvariant,

            /// <remarks/>
            [XmlEnum("Latn - Latin")]
            LatnLatin,

            /// <remarks/>
            [XmlEnum("Leke - Leke")]
            LekeLeke,

            /// <remarks/>
            [XmlEnum("Lepc - Lepcha (Róng)")]
            LepcLepchaRóng,

            /// <remarks/>
            [XmlEnum("Limb - Limbu")]
            LimbLimbu,

            /// <remarks/>
            [XmlEnum("Lina - Linear A")]
            LinaLinearA,

            /// <remarks/>
            [XmlEnum("Linb - Linear B")]
            LinbLinearB,

            /// <remarks/>
            [XmlEnum("Lisu - Lisu (Fraser)")]
            LisuLisuFraser,

            /// <remarks/>
            [XmlEnum("Loma - Loma")]
            LomaLoma,

            /// <remarks/>
            [XmlEnum("Lyci - Lycian")]
            LyciLycian,

            /// <remarks/>
            [XmlEnum("Lydi - Lydian")]
            LydiLydian,

            /// <remarks/>
            [XmlEnum("Mahj - Mahajani")]
            MahjMahajani,

            /// <remarks/>
            [XmlEnum("Mand - Mandaic, Mandaean")]
            MandMandaicMandaean,

            /// <remarks/>
            [XmlEnum("Mani - Manichaean")]
            ManiManichaean,

            /// <remarks/>
            [XmlEnum("Marc - Marchen")]
            MarcMarchen,

            /// <remarks/>
            [XmlEnum("Maya - Mayan hieroglyphs")]
            MayaMayanhieroglyphs,

            /// <remarks/>
            [XmlEnum("Mend - Mende Kikakui")]
            MendMendeKikakui,

            /// <remarks/>
            [XmlEnum("Merc - Meroitic Cursive")]
            MercMeroiticCursive,

            /// <remarks/>
            [XmlEnum("Mero - Meroitic Hieroglyphs")]
            MeroMeroiticHieroglyphs,

            /// <remarks/>
            [XmlEnum("Mlym - Malayalam")]
            MlymMalayalam,

            /// <remarks/>
            [XmlEnum("Modi - Modi, Moḍī")]
            ModiModiMoḍī,

            /// <remarks/>
            [XmlEnum("Mong - Mongolian")]
            MongMongolian,

            /// <remarks/>
            [XmlEnum("Moon - Moon (Moon code, Moon script, Moon type)")]
            MoonMoonMooncodeMoonscriptMoontype,

            /// <remarks/>
            [XmlEnum("Mroo - Mro, Mru")]
            MrooMroMru,

            /// <remarks/>
            [XmlEnum("Mtei - Meitei Mayek (Meithei, Meetei)")]
            MteiMeiteiMayekMeitheiMeetei,

            /// <remarks/>
            [XmlEnum("Mult - Multani")]
            MultMultani,

            /// <remarks/>
            [XmlEnum("Mymr - Myanmar (Burmese)")]
            MymrMyanmarBurmese,

            /// <remarks/>
            [XmlEnum("Narb - Old North Arabian (Ancient North Arabian)")]
            NarbOldNorthArabianAncientNorthArabian,

            /// <remarks/>
            [XmlEnum("Nbat - Nabataean")]
            NbatNabataean,

            /// <remarks/>
            [XmlEnum("Newa - Newa, Newar, Newari")]
            NewaNewaNewarNewari,

            /// <remarks/>
            [XmlEnum("Nkgb - Nakhi Geba")]
            NkgbNakhiGeba,

            /// <remarks/>
            [XmlEnum("Nkoo - N’Ko")]
            NkooNKo,

            /// <remarks/>
            [XmlEnum("Nshu - Nüshu")]
            NshuNüshu,

            /// <remarks/>
            [XmlEnum("Ogam - Ogham")]
            OgamOgham,

            /// <remarks/>
            [XmlEnum("Olck - Ol Chiki (Ol Cemet’, Ol, Santali)")]
            OlckOlChikiOlCemetOlSantali,

            /// <remarks/>
            [XmlEnum("Orkh - Old Turkic, Orkhon Runic")]
            OrkhOldTurkicOrkhonRunic,

            /// <remarks/>
            [XmlEnum("Orya - Oriya")]
            OryaOriya,

            /// <remarks/>
            [XmlEnum("Osge - Osage")]
            OsgeOsage,

            /// <remarks/>
            [XmlEnum("Osma - Osmanya")]
            OsmaOsmanya,

            /// <remarks/>
            [XmlEnum("Palm - Palmyrene")]
            PalmPalmyrene,

            /// <remarks/>
            [XmlEnum("Pauc - Pau Cin Hau")]
            PaucPauCinHau,

            /// <remarks/>
            [XmlEnum("Perm - Old Permic")]
            PermOldPermic,

            /// <remarks/>
            [XmlEnum("Phag - Phags-pa")]
            PhagPhagspa,

            /// <remarks/>
            [XmlEnum("Phli - Inscriptional Pahlavi")]
            PhliInscriptionalPahlavi,

            /// <remarks/>
            [XmlEnum("Phlp - Psalter Pahlavi")]
            PhlpPsalterPahlavi,

            /// <remarks/>
            [XmlEnum("Phlv - Book Pahlavi")]
            PhlvBookPahlavi,

            /// <remarks/>
            [XmlEnum("Phnx - Phoenician")]
            PhnxPhoenician,

            /// <remarks/>
            [XmlEnum("Piqd - Klingon (KLI pIqaD)")]
            PiqdKlingonKLIpIqaD,

            /// <remarks/>
            [XmlEnum("Plrd - Miao (Pollard)")]
            PlrdMiaoPollard,

            /// <remarks/>
            [XmlEnum("Prti - Inscriptional Parthian")]
            PrtiInscriptionalParthian,

            /// <remarks/>
            [XmlEnum("Rjng - Rejang (Redjang, Kaganga)")]
            RjngRejangRedjangKaganga,

            /// <remarks/>
            [XmlEnum("Roro - Rongorongo")]
            RoroRongorongo,

            /// <remarks/>
            [XmlEnum("Runr - Runic")]
            RunrRunic,

            /// <remarks/>
            [XmlEnum("Samr - Samaritan")]
            SamrSamaritan,

            /// <remarks/>
            [XmlEnum("Sara - Sarati")]
            SaraSarati,

            /// <remarks/>
            [XmlEnum("Sarb - Old South Arabian")]
            SarbOldSouthArabian,

            /// <remarks/>
            [XmlEnum("Saur - Saurashtra")]
            SaurSaurashtra,

            /// <remarks/>
            [XmlEnum("Sgnw - SignWriting")]
            SgnwSignWriting,

            /// <remarks/>
            [XmlEnum("Shaw - Shavian (Shaw)")]
            ShawShavianShaw,

            /// <remarks/>
            [XmlEnum("Shrd - Sharada, Śāradā")]
            ShrdSharadaŚāradā,

            /// <remarks/>
            [XmlEnum("Sidd - Siddham")]
            SiddSiddham,

            /// <remarks/>
            [XmlEnum("Sind - Khudawadi, Sindhi")]
            SindKhudawadiSindhi,

            /// <remarks/>
            [XmlEnum("Sinh - Sinhala")]
            SinhSinhala,

            /// <remarks/>
            [XmlEnum("Sora - Sora Sompeng")]
            SoraSoraSompeng,

            /// <remarks/>
            [XmlEnum("Sund - Sundanese")]
            SundSundanese,

            /// <remarks/>
            [XmlEnum("Sylo - Syloti Nagri")]
            SyloSylotiNagri,

            /// <remarks/>
            [XmlEnum("Syrc - Syriac")]
            SyrcSyriac,

            /// <remarks/>
            [XmlEnum("Syre - Syriac (Estrangelo variant)")]
            SyreSyriacEstrangeloVariant,

            /// <remarks/>
            [XmlEnum("Syrj - Syriac (Western variant)")]
            SyrjSyriacWesternVariant,

            /// <remarks/>
            [XmlEnum("Syrn - Syriac (Eastern variant)")]
            SyrnSyriacEasternVariant,

            /// <remarks/>
            [XmlEnum("Tagb - Tagbanwa")]
            TagbTagbanwa,

            /// <remarks/>
            [XmlEnum("Takr - Takri")]
            TakrTakri,

            /// <remarks/>
            [XmlEnum("Tale - Tai Le")]
            TaleTaiLe,

            /// <remarks/>
            [XmlEnum("Talu - New Tai Lue")]
            TaluNewTaiLue,

            /// <remarks/>
            [XmlEnum("Taml - Tamil")]
            TamlTamil,

            /// <remarks/>
            [XmlEnum("Tang - Tangut")]
            TangTangut,

            /// <remarks/>
            [XmlEnum("Tavt - Tai Viet")]
            TavtTaiViet,

            /// <remarks/>
            [XmlEnum("Telu - Telugu")]
            TeluTelugu,

            /// <remarks/>
            [XmlEnum("Teng - Tengwar")]
            TengTengwar,

            /// <remarks/>
            [XmlEnum("Tfng - Tifinagh (Berber)")]
            TfngTifinaghBerber,

            /// <remarks/>
            [XmlEnum("Tglg - Tagalog (Baybayin, Alibata)")]
            TglgTagalogBaybayinAlibata,

            /// <remarks/>
            [XmlEnum("Thaa - Thaana")]
            ThaaThaana,

            /// <remarks/>
            [XmlEnum("Thai - Thai")]
            ThaiThai,

            /// <remarks/>
            [XmlEnum("Tibt - Tibetan")]
            TibtTibetan,

            /// <remarks/>
            [XmlEnum("Tirh - Tirhuta")]
            TirhTirhuta,

            /// <remarks/>
            [XmlEnum("Ugar - Ugaritic")]
            UgarUgaritic,

            /// <remarks/>
            [XmlEnum("Vaii - Vai")]
            VaiiVai,

            /// <remarks/>
            [XmlEnum("Visp - Visible Speech")]
            VispVisibleSpeech,

            /// <remarks/>
            [XmlEnum("Wara - Warang Citi (Varang Kshiti)")]
            WaraWarangCitiVarangKshiti,

            /// <remarks/>
            [XmlEnum("Wole - Woleai")]
            WoleWoleai,

            /// <remarks/>
            [XmlEnum("Xpeo - Old Persian")]
            XpeoOldPersian,

            /// <remarks/>
            [XmlEnum("Xsux - Cuneiform, Sumero-Akkadian")]
            XsuxCuneiformSumeroAkkadian,

            /// <remarks/>
            [XmlEnum("Yiii - Yi")]
            YiiiYi,

            /// <remarks/>
            [XmlEnum("Zinh - Code for inherited script")]
            ZinhCodeForInheritedScript,

            /// <remarks/>
            [XmlEnum("Zmth - Mathematical notation")]
            ZmthMathematicalNotation,

            /// <remarks/>
            [XmlEnum("Zsye - Symbols (Emoji variant)")]
            ZsyeSymbolsEmojiVariant,

            /// <remarks/>
            [XmlEnum("Zsym - Symbols")]
            ZsymSymbols,

            /// <remarks/>
            [XmlEnum("Zxxx - Code for unwritten documents")]
            ZxxxCodeForUnwrittenDocuments,

            /// <remarks/>
            [XmlEnum("Zyyy - Code for undetermined script")]
            ZyyyCodeForUndeterminedScript,

            /// <remarks/>
            [XmlEnum("Zzzz - Code for uncoded script")]
            ZzzzCodeForUncodedScript,

            /// <remarks/>
            [XmlEnum("other")]
            Other,
        }
    }
}
