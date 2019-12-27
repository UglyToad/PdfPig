namespace UglyToad.PdfPig.Export.PAGE
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
        [GeneratedCodeAttribute("xsd", "4.6.1055.0")]
        [SerializableAttribute()]
        [XmlTypeAttribute(Namespace = "http://schema.primaresearch.org/PAGE/gts/pagecontent/2019-07-15")]
        public enum PageXmlScriptSimpleType
        {

            /// <remarks/>
            [XmlEnumAttribute("Adlm - Adlam")]
            AdlmAdlam,

            /// <remarks/>
            [XmlEnumAttribute("Afak - Afaka")]
            AfakAfaka,

            /// <remarks/>
            [XmlEnumAttribute("Aghb - Caucasian Albanian")]
            AghbCaucasianAlbanian,

            /// <remarks/>
            [XmlEnumAttribute("Ahom - Ahom, Tai Ahom")]
            AhomAhomTaiAhom,

            /// <remarks/>
            [XmlEnumAttribute("Arab - Arabic")]
            ArabArabic,

            /// <remarks/>
            [XmlEnumAttribute("Aran - Arabic (Nastaliq variant)")]
            AranArabicNastaliqVariant,

            /// <remarks/>
            [XmlEnumAttribute("Armi - Imperial Aramaic")]
            ArmiImperialAramaic,

            /// <remarks/>
            [XmlEnumAttribute("Armn - Armenian")]
            ArmnArmenian,

            /// <remarks/>
            [XmlEnumAttribute("Avst - Avestan")]
            AvstAvestan,

            /// <remarks/>
            [XmlEnumAttribute("Bali - Balinese")]
            BaliBalinese,

            /// <remarks/>
            [XmlEnumAttribute("Bamu - Bamum")]
            BamuBamum,

            /// <remarks/>
            [XmlEnumAttribute("Bass - Bassa Vah")]
            BassBassaVah,

            /// <remarks/>
            [XmlEnumAttribute("Batk - Batak")]
            BatkBatak,

            /// <remarks/>
            [XmlEnumAttribute("Beng - Bengali")]
            BengBengali,

            /// <remarks/>
            [XmlEnumAttribute("Bhks - Bhaiksuki")]
            BhksBhaiksuki,

            /// <remarks/>
            [XmlEnumAttribute("Blis - Blissymbols")]
            BlisBlissymbols,

            /// <remarks/>
            [XmlEnumAttribute("Bopo - Bopomofo")]
            BopoBopomofo,

            /// <remarks/>
            [XmlEnumAttribute("Brah - Brahmi")]
            BrahBrahmi,

            /// <remarks/>
            [XmlEnumAttribute("Brai - Braille")]
            BraiBraille,

            /// <remarks/>
            [XmlEnumAttribute("Bugi - Buginese")]
            BugiBuginese,

            /// <remarks/>
            [XmlEnumAttribute("Buhd - Buhid")]
            BuhdBuhid,

            /// <remarks/>
            [XmlEnumAttribute("Cakm - Chakma")]
            CakmChakma,

            /// <remarks/>
            [XmlEnumAttribute("Cans - Unified Canadian Aboriginal Syllabics")]
            CansUnifiedCanadianAboriginalSyllabics,

            /// <remarks/>
            [XmlEnumAttribute("Cari - Carian")]
            CariCarian,

            /// <remarks/>
            [XmlEnumAttribute("Cham - Cham")]
            ChamCham,

            /// <remarks/>
            [XmlEnumAttribute("Cher - Cherokee")]
            CherCherokee,

            /// <remarks/>
            [XmlEnumAttribute("Cirt - Cirth")]
            CirtCirth,

            /// <remarks/>
            [XmlEnumAttribute("Copt - Coptic")]
            CoptCoptic,

            /// <remarks/>
            [XmlEnumAttribute("Cprt - Cypriot")]
            CprtCypriot,

            /// <remarks/>
            [XmlEnumAttribute("Cyrl - Cyrillic")]
            CyrlCyrillic,

            /// <remarks/>
            [XmlEnumAttribute("Cyrs - Cyrillic (Old Church Slavonic variant)")]
            CyrsCyrillicOldChurchSlavonicVariant,

            /// <remarks/>
            [XmlEnumAttribute("Deva - Devanagari (Nagari)")]
            DevaDevanagariNagari,

            /// <remarks/>
            [XmlEnumAttribute("Dsrt - Deseret (Mormon)")]
            DsrtDeseretMormon,

            /// <remarks/>
            [XmlEnumAttribute("Dupl - Duployan shorthand, Duployan stenography")]
            DuplDuployanShorthandDuployanStenography,

            /// <remarks/>
            [XmlEnumAttribute("Egyd - Egyptian demotic")]
            EgydEgyptianDemotic,

            /// <remarks/>
            [XmlEnumAttribute("Egyh - Egyptian hieratic")]
            EgyhEgyptianHieratic,

            /// <remarks/>
            [XmlEnumAttribute("Egyp - Egyptian hieroglyphs")]
            EgypEgyptianHieroglyphs,

            /// <remarks/>
            [XmlEnumAttribute("Elba - Elbasan")]
            ElbaElbasan,

            /// <remarks/>
            [XmlEnumAttribute("Ethi - Ethiopic")]
            EthiEthiopic,

            /// <remarks/>
            [XmlEnumAttribute("Geok - Khutsuri (Asomtavruli and Nuskhuri)")]
            GeokKhutsuriAsomtavruliAndNuskhuri,

            /// <remarks/>
            [XmlEnumAttribute("Geor - Georgian (Mkhedruli)")]
            GeorGeorgianMkhedruli,

            /// <remarks/>
            [XmlEnumAttribute("Glag - Glagolitic")]
            GlagGlagolitic,

            /// <remarks/>
            [XmlEnumAttribute("Goth - Gothic")]
            GothGothic,

            /// <remarks/>
            [XmlEnumAttribute("Gran - Grantha")]
            GranGrantha,

            /// <remarks/>
            [XmlEnumAttribute("Grek - Greek")]
            GrekGreek,

            /// <remarks/>
            [XmlEnumAttribute("Gujr - Gujarati")]
            GujrGujarati,

            /// <remarks/>
            [XmlEnumAttribute("Guru - Gurmukhi")]
            GuruGurmukhi,

            /// <remarks/>
            [XmlEnumAttribute("Hanb - Han with Bopomofo")]
            HanbHanwithBopomofo,

            /// <remarks/>
            [XmlEnumAttribute("Hang - Hangul")]
            HangHangul,

            /// <remarks/>
            [XmlEnumAttribute("Hani - Han (Hanzi, Kanji, Hanja)")]
            HaniHanHanziKanjiHanja,

            /// <remarks/>
            [XmlEnumAttribute("Hano - Hanunoo (Hanunóo)")]
            HanoHanunooHanunóo,

            /// <remarks/>
            [XmlEnumAttribute("Hans - Han (Simplified variant)")]
            HansHanSimplifiedVariant,

            /// <remarks/>
            [XmlEnumAttribute("Hant - Han (Traditional variant)")]
            HantHanTraditionalVariant,

            /// <remarks/>
            [XmlEnumAttribute("Hatr - Hatran")]
            HatrHatran,

            /// <remarks/>
            [XmlEnumAttribute("Hebr - Hebrew")]
            HebrHebrew,

            /// <remarks/>
            [XmlEnumAttribute("Hira - Hiragana")]
            HiraHiragana,

            /// <remarks/>
            [XmlEnumAttribute("Hluw - Anatolian Hieroglyphs")]
            HluwAnatolianHieroglyphs,

            /// <remarks/>
            [XmlEnumAttribute("Hmng - Pahawh Hmong")]
            HmngPahawhHmong,

            /// <remarks/>
            [XmlEnumAttribute("Hrkt - Japanese syllabaries")]
            HrktJapaneseSyllabaries,

            /// <remarks/>
            [XmlEnumAttribute("Hung - Old Hungarian (Hungarian Runic)")]
            HungOldHungarianHungarianRunic,

            /// <remarks/>
            [XmlEnumAttribute("Inds - Indus (Harappan)")]
            IndsIndusHarappan,

            /// <remarks/>
            [XmlEnumAttribute("Ital - Old Italic (Etruscan, Oscan etc.)")]
            ItalOldItalicEtruscanOscanEtc,

            /// <remarks/>
            [XmlEnumAttribute("Jamo - Jamo")]
            JamoJamo,

            /// <remarks/>
            [XmlEnumAttribute("Java - Javanese")]
            JavaJavanese,

            /// <remarks/>
            [XmlEnumAttribute("Jpan - Japanese")]
            JpanJapanese,

            /// <remarks/>
            [XmlEnumAttribute("Jurc - Jurchen")]
            JurcJurchen,

            /// <remarks/>
            [XmlEnumAttribute("Kali - Kayah Li")]
            KaliKayahLi,

            /// <remarks/>
            [XmlEnumAttribute("Kana - Katakana")]
            KanaKatakana,

            /// <remarks/>
            [XmlEnumAttribute("Khar - Kharoshthi")]
            KharKharoshthi,

            /// <remarks/>
            [XmlEnumAttribute("Khmr - Khmer")]
            KhmrKhmer,

            /// <remarks/>
            [XmlEnumAttribute("Khoj - Khojki")]
            KhojKhojki,

            /// <remarks/>
            [XmlEnumAttribute("Kitl - Khitan large script")]
            KitlKhitanlargescript,

            /// <remarks/>
            [XmlEnumAttribute("Kits - Khitan small script")]
            KitsKhitansmallscript,

            /// <remarks/>
            [XmlEnumAttribute("Knda - Kannada")]
            KndaKannada,

            /// <remarks/>
            [XmlEnumAttribute("Kore - Korean (alias for Hangul + Han)")]
            KoreKoreanaliasforHangulHan,

            /// <remarks/>
            [XmlEnumAttribute("Kpel - Kpelle")]
            KpelKpelle,

            /// <remarks/>
            [XmlEnumAttribute("Kthi - Kaithi")]
            KthiKaithi,

            /// <remarks/>
            [XmlEnumAttribute("Lana - Tai Tham (Lanna)")]
            LanaTaiThamLanna,

            /// <remarks/>
            [XmlEnumAttribute("Laoo - Lao")]
            LaooLao,

            /// <remarks/>
            [XmlEnumAttribute("Latf - Latin (Fraktur variant)")]
            LatfLatinFrakturvariant,

            /// <remarks/>
            [XmlEnumAttribute("Latg - Latin (Gaelic variant)")]
            LatgLatinGaelicvariant,

            /// <remarks/>
            [XmlEnumAttribute("Latn - Latin")]
            LatnLatin,

            /// <remarks/>
            [XmlEnumAttribute("Leke - Leke")]
            LekeLeke,

            /// <remarks/>
            [XmlEnumAttribute("Lepc - Lepcha (Róng)")]
            LepcLepchaRóng,

            /// <remarks/>
            [XmlEnumAttribute("Limb - Limbu")]
            LimbLimbu,

            /// <remarks/>
            [XmlEnumAttribute("Lina - Linear A")]
            LinaLinearA,

            /// <remarks/>
            [XmlEnumAttribute("Linb - Linear B")]
            LinbLinearB,

            /// <remarks/>
            [XmlEnumAttribute("Lisu - Lisu (Fraser)")]
            LisuLisuFraser,

            /// <remarks/>
            [XmlEnumAttribute("Loma - Loma")]
            LomaLoma,

            /// <remarks/>
            [XmlEnumAttribute("Lyci - Lycian")]
            LyciLycian,

            /// <remarks/>
            [XmlEnumAttribute("Lydi - Lydian")]
            LydiLydian,

            /// <remarks/>
            [XmlEnumAttribute("Mahj - Mahajani")]
            MahjMahajani,

            /// <remarks/>
            [XmlEnumAttribute("Mand - Mandaic, Mandaean")]
            MandMandaicMandaean,

            /// <remarks/>
            [XmlEnumAttribute("Mani - Manichaean")]
            ManiManichaean,

            /// <remarks/>
            [XmlEnumAttribute("Marc - Marchen")]
            MarcMarchen,

            /// <remarks/>
            [XmlEnumAttribute("Maya - Mayan hieroglyphs")]
            MayaMayanhieroglyphs,

            /// <remarks/>
            [XmlEnumAttribute("Mend - Mende Kikakui")]
            MendMendeKikakui,

            /// <remarks/>
            [XmlEnumAttribute("Merc - Meroitic Cursive")]
            MercMeroiticCursive,

            /// <remarks/>
            [XmlEnumAttribute("Mero - Meroitic Hieroglyphs")]
            MeroMeroiticHieroglyphs,

            /// <remarks/>
            [XmlEnumAttribute("Mlym - Malayalam")]
            MlymMalayalam,

            /// <remarks/>
            [XmlEnumAttribute("Modi - Modi, Moḍī")]
            ModiModiMoḍī,

            /// <remarks/>
            [XmlEnumAttribute("Mong - Mongolian")]
            MongMongolian,

            /// <remarks/>
            [XmlEnumAttribute("Moon - Moon (Moon code, Moon script, Moon type)")]
            MoonMoonMooncodeMoonscriptMoontype,

            /// <remarks/>
            [XmlEnumAttribute("Mroo - Mro, Mru")]
            MrooMroMru,

            /// <remarks/>
            [XmlEnumAttribute("Mtei - Meitei Mayek (Meithei, Meetei)")]
            MteiMeiteiMayekMeitheiMeetei,

            /// <remarks/>
            [XmlEnumAttribute("Mult - Multani")]
            MultMultani,

            /// <remarks/>
            [XmlEnumAttribute("Mymr - Myanmar (Burmese)")]
            MymrMyanmarBurmese,

            /// <remarks/>
            [XmlEnumAttribute("Narb - Old North Arabian (Ancient North Arabian)")]
            NarbOldNorthArabianAncientNorthArabian,

            /// <remarks/>
            [XmlEnumAttribute("Nbat - Nabataean")]
            NbatNabataean,

            /// <remarks/>
            [XmlEnumAttribute("Newa - Newa, Newar, Newari")]
            NewaNewaNewarNewari,

            /// <remarks/>
            [XmlEnumAttribute("Nkgb - Nakhi Geba")]
            NkgbNakhiGeba,

            /// <remarks/>
            [XmlEnumAttribute("Nkoo - N’Ko")]
            NkooNKo,

            /// <remarks/>
            [XmlEnumAttribute("Nshu - Nüshu")]
            NshuNüshu,

            /// <remarks/>
            [XmlEnumAttribute("Ogam - Ogham")]
            OgamOgham,

            /// <remarks/>
            [XmlEnumAttribute("Olck - Ol Chiki (Ol Cemet’, Ol, Santali)")]
            OlckOlChikiOlCemetOlSantali,

            /// <remarks/>
            [XmlEnumAttribute("Orkh - Old Turkic, Orkhon Runic")]
            OrkhOldTurkicOrkhonRunic,

            /// <remarks/>
            [XmlEnumAttribute("Orya - Oriya")]
            OryaOriya,

            /// <remarks/>
            [XmlEnumAttribute("Osge - Osage")]
            OsgeOsage,

            /// <remarks/>
            [XmlEnumAttribute("Osma - Osmanya")]
            OsmaOsmanya,

            /// <remarks/>
            [XmlEnumAttribute("Palm - Palmyrene")]
            PalmPalmyrene,

            /// <remarks/>
            [XmlEnumAttribute("Pauc - Pau Cin Hau")]
            PaucPauCinHau,

            /// <remarks/>
            [XmlEnumAttribute("Perm - Old Permic")]
            PermOldPermic,

            /// <remarks/>
            [XmlEnumAttribute("Phag - Phags-pa")]
            PhagPhagspa,

            /// <remarks/>
            [XmlEnumAttribute("Phli - Inscriptional Pahlavi")]
            PhliInscriptionalPahlavi,

            /// <remarks/>
            [XmlEnumAttribute("Phlp - Psalter Pahlavi")]
            PhlpPsalterPahlavi,

            /// <remarks/>
            [XmlEnumAttribute("Phlv - Book Pahlavi")]
            PhlvBookPahlavi,

            /// <remarks/>
            [XmlEnumAttribute("Phnx - Phoenician")]
            PhnxPhoenician,

            /// <remarks/>
            [XmlEnumAttribute("Piqd - Klingon (KLI pIqaD)")]
            PiqdKlingonKLIpIqaD,

            /// <remarks/>
            [XmlEnumAttribute("Plrd - Miao (Pollard)")]
            PlrdMiaoPollard,

            /// <remarks/>
            [XmlEnumAttribute("Prti - Inscriptional Parthian")]
            PrtiInscriptionalParthian,

            /// <remarks/>
            [XmlEnumAttribute("Rjng - Rejang (Redjang, Kaganga)")]
            RjngRejangRedjangKaganga,

            /// <remarks/>
            [XmlEnumAttribute("Roro - Rongorongo")]
            RoroRongorongo,

            /// <remarks/>
            [XmlEnumAttribute("Runr - Runic")]
            RunrRunic,

            /// <remarks/>
            [XmlEnumAttribute("Samr - Samaritan")]
            SamrSamaritan,

            /// <remarks/>
            [XmlEnumAttribute("Sara - Sarati")]
            SaraSarati,

            /// <remarks/>
            [XmlEnumAttribute("Sarb - Old South Arabian")]
            SarbOldSouthArabian,

            /// <remarks/>
            [XmlEnumAttribute("Saur - Saurashtra")]
            SaurSaurashtra,

            /// <remarks/>
            [XmlEnumAttribute("Sgnw - SignWriting")]
            SgnwSignWriting,

            /// <remarks/>
            [XmlEnumAttribute("Shaw - Shavian (Shaw)")]
            ShawShavianShaw,

            /// <remarks/>
            [XmlEnumAttribute("Shrd - Sharada, Śāradā")]
            ShrdSharadaŚāradā,

            /// <remarks/>
            [XmlEnumAttribute("Sidd - Siddham")]
            SiddSiddham,

            /// <remarks/>
            [XmlEnumAttribute("Sind - Khudawadi, Sindhi")]
            SindKhudawadiSindhi,

            /// <remarks/>
            [XmlEnumAttribute("Sinh - Sinhala")]
            SinhSinhala,

            /// <remarks/>
            [XmlEnumAttribute("Sora - Sora Sompeng")]
            SoraSoraSompeng,

            /// <remarks/>
            [XmlEnumAttribute("Sund - Sundanese")]
            SundSundanese,

            /// <remarks/>
            [XmlEnumAttribute("Sylo - Syloti Nagri")]
            SyloSylotiNagri,

            /// <remarks/>
            [XmlEnumAttribute("Syrc - Syriac")]
            SyrcSyriac,

            /// <remarks/>
            [XmlEnumAttribute("Syre - Syriac (Estrangelo variant)")]
            SyreSyriacEstrangeloVariant,

            /// <remarks/>
            [XmlEnumAttribute("Syrj - Syriac (Western variant)")]
            SyrjSyriacWesternVariant,

            /// <remarks/>
            [XmlEnumAttribute("Syrn - Syriac (Eastern variant)")]
            SyrnSyriacEasternVariant,

            /// <remarks/>
            [XmlEnumAttribute("Tagb - Tagbanwa")]
            TagbTagbanwa,

            /// <remarks/>
            [XmlEnumAttribute("Takr - Takri")]
            TakrTakri,

            /// <remarks/>
            [XmlEnumAttribute("Tale - Tai Le")]
            TaleTaiLe,

            /// <remarks/>
            [XmlEnumAttribute("Talu - New Tai Lue")]
            TaluNewTaiLue,

            /// <remarks/>
            [XmlEnumAttribute("Taml - Tamil")]
            TamlTamil,

            /// <remarks/>
            [XmlEnumAttribute("Tang - Tangut")]
            TangTangut,

            /// <remarks/>
            [XmlEnumAttribute("Tavt - Tai Viet")]
            TavtTaiViet,

            /// <remarks/>
            [XmlEnumAttribute("Telu - Telugu")]
            TeluTelugu,

            /// <remarks/>
            [XmlEnumAttribute("Teng - Tengwar")]
            TengTengwar,

            /// <remarks/>
            [XmlEnumAttribute("Tfng - Tifinagh (Berber)")]
            TfngTifinaghBerber,

            /// <remarks/>
            [XmlEnumAttribute("Tglg - Tagalog (Baybayin, Alibata)")]
            TglgTagalogBaybayinAlibata,

            /// <remarks/>
            [XmlEnumAttribute("Thaa - Thaana")]
            ThaaThaana,

            /// <remarks/>
            [XmlEnumAttribute("Thai - Thai")]
            ThaiThai,

            /// <remarks/>
            [XmlEnumAttribute("Tibt - Tibetan")]
            TibtTibetan,

            /// <remarks/>
            [XmlEnumAttribute("Tirh - Tirhuta")]
            TirhTirhuta,

            /// <remarks/>
            [XmlEnumAttribute("Ugar - Ugaritic")]
            UgarUgaritic,

            /// <remarks/>
            [XmlEnumAttribute("Vaii - Vai")]
            VaiiVai,

            /// <remarks/>
            [XmlEnumAttribute("Visp - Visible Speech")]
            VispVisibleSpeech,

            /// <remarks/>
            [XmlEnumAttribute("Wara - Warang Citi (Varang Kshiti)")]
            WaraWarangCitiVarangKshiti,

            /// <remarks/>
            [XmlEnumAttribute("Wole - Woleai")]
            WoleWoleai,

            /// <remarks/>
            [XmlEnumAttribute("Xpeo - Old Persian")]
            XpeoOldPersian,

            /// <remarks/>
            [XmlEnumAttribute("Xsux - Cuneiform, Sumero-Akkadian")]
            XsuxCuneiformSumeroAkkadian,

            /// <remarks/>
            [XmlEnumAttribute("Yiii - Yi")]
            YiiiYi,

            /// <remarks/>
            [XmlEnumAttribute("Zinh - Code for inherited script")]
            ZinhCodeForInheritedScript,

            /// <remarks/>
            [XmlEnumAttribute("Zmth - Mathematical notation")]
            ZmthMathematicalNotation,

            /// <remarks/>
            [XmlEnumAttribute("Zsye - Symbols (Emoji variant)")]
            ZsyeSymbolsEmojiVariant,

            /// <remarks/>
            [XmlEnumAttribute("Zsym - Symbols")]
            ZsymSymbols,

            /// <remarks/>
            [XmlEnumAttribute("Zxxx - Code for unwritten documents")]
            ZxxxCodeForUnwrittenDocuments,

            /// <remarks/>
            [XmlEnumAttribute("Zyyy - Code for undetermined script")]
            ZyyyCodeForUndeterminedScript,

            /// <remarks/>
            [XmlEnumAttribute("Zzzz - Code for uncoded script")]
            ZzzzCodeForUncodedScript,

            /// <remarks/>
            [XmlEnumAttribute("other")]
            Other,
        }
    }
}
