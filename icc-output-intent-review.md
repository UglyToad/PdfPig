# ICC Profile & Output Intent Support — PdfPig vs PDFBox

**Branch reviewed:** `feature/icc-profile-support-4` @ `f5b8a079` ("WIP - Add ICC profiles color spaces and output intent support, and add tests")
**Compared against:** `C:\Users\Bob\source\repos\pdfbox` @ `bbea338208`
**Date:** 2026-08-09
**Scope:** ICC-based colour spaces, output intents, and the supporting plumbing (colour space parsing, caching, image byte conversion, rendering intent).

> **Status update.** Twelve findings have since been addressed in the working tree, each marked ✅ **Fixed** below with what was done:
>
> - *Fix before merge:* **A1**, **B4**, **B7**
> - *Correctness, medium priority:* **B9**, **B10**, **B5**, **B12**, **B11**
> - *High value, follows PDFBox closely:* **B2**, **B6**, **B8**, **A2**
> - *API shape:* **A3**, **A5**, **A7**
>
> **B3** (sRGB fast path) has been reclassified: it is not PdfPig's to fix but the ICC backend's, for the reasons set out in its section. Still open on PdfPig: **A4** (surface output intents on the document catalog) and **B1** (ship or document a default `IIccProfileService`). Full test suite after the fixes: 4284 passed, 0 failed, 7 skipped; all target frameworks build clean.

---

## 1. Executive summary

The branch adds two things that PDFBox does *not* have, and leaves out several things PDFBox *does* have.

**Fundamental architectural difference.** PDFBox does not implement ICC colour management — it delegates to `java.awt.color.ICC_ColorSpace`, which is backed by the JDK's bundled CMM (LittleCMS since JDK 8). Everything in `PDICCBased` is glue: load bytes, hand to the platform, catch the platform's exceptions, fall back to the alternate. PdfPig has no platform CMM available, so this branch instead defines a **provider interface** (`IIccProfileService` → `IIccProfile` → `IIccTransform`) that a caller must supply. That is the right call for .NET, and most of the differences below flow from it.

**The consequence that matters most:** PDFBox colour-manages ICCBased content **by default**; PdfPig colour-manages it **only if the caller sets `ParsingOptions.IccProfileService`**, which defaults to `null`. Out of the box, PdfPig on this branch behaves exactly as it did before — always the alternate colour space. PDFBox's equivalent behaviour is opt-*out*, via an undocumented system property that its own source labels *"WARNING: do not activate this in a conforming reader"* (`PDICCBased.java:66-69`).

**Two things PdfPig does that PDFBox does not:**

1. **Rendering intent is threaded end-to-end.** PDFBox parses `ri`/`/RI` into `PDGraphicsState.renderingIntent` and then *never reads it* — a grep for `getRenderingIntent()` across `pdfbox/src/main` returns only the getter, the setter, and the ExtGState copy. PdfPig now carries `RenderingIntent` through `GetColor`, `GetRgb`, `Process`, `Transform`, `ColorSpaceDetailsByteConverter.Convert`, and into `IPdfImage`. This is a genuine improvement and the single biggest API-surface divergence.
2. **Output intents are parsed for reading.** PDFBox's `PDOutputIntent` is a write-side/model-only class (used by `CreatePDFA` and `PDFMergerUtility`); it is never consulted during rendering. PdfPig parses it, resolves and decodes `/DestOutputProfile`, ranks candidates, and supports page-level `/OutputIntents` (PDF 2.0 Table 31), which PDFBox has no support for at all.

**But** — see finding **A1** — the parsed output intent has **no consumer**. Nothing reads `CurrentGraphicsState.OutputIntent` or `IResourceStore.OutputIntent`, while the XML docs on both stated it was *"Used to colour-manage the device colour spaces (DeviceCMYK / DeviceRGB / DeviceGray) per PDF/X semantics."* That behaviour is not implemented; the docs now say so.

**Highest-value gaps against PDFBox**, in order: ~~`/Alternate` arrays are silently ignored (**B4**)~~ ✅; ~~a wrong `/N` disables colour management instead of being corrected (**B2**)~~ ✅; ~~no logging on any fallback (**B6**)~~ ✅; ~~parsed profiles are not cached across resource dictionaries (**B8**)~~ ✅. The sRGB fast path (**B3**) was on this list too, but it is a backend concern rather than a PdfPig one — see its section.

**Fixed along the way, and worth calling out separately:** every **Lab image rendered near-black**, because `LabColorSpaceDetails.Transform` divided its samples by 255 and handed `GetRgb` an L\* of at most 1 (of 100). This was independent of ICC and predates the branch; it surfaced while making the image and scalar paths agree (**B9**).

---

## 2. File map

| Concern | PdfPig | PDFBox |
|---|---|---|
| ICCBased colour space | `Graphics/Colors/ColorSpaces/ICCBasedColorSpaceDetails.cs` | `pdmodel/graphics/color/PDICCBased.java` |
| Colour space dispatch | `Util/ColorSpaceDetailsParser.cs` | `pdmodel/graphics/color/PDColorSpace.java` (`create`) |
| Output intent model | `Graphics/Colors/Icc/OutputIntent.cs` | `pdmodel/graphics/color/PDOutputIntent.java` |
| Output intent parsing | `Util/OutputIntentParser.cs` | `pdmodel/PDDocumentCatalog.getOutputIntents()` |
| Output intent writing | `Writer/Colors/OutputIntentsFactory.cs` | `PDOutputIntent(PDDocument, InputStream)` |
| CMM abstraction | `Graphics/Colors/Icc/IIccProfile*.cs`, `IIccTransform.cs` | *(none — `java.awt.color.ICC_ColorSpace`)* |
| Profile caching | `Util/IccProfileCache.cs` | `pdmodel/ResourceCache` (caches the `PDICCBased` object) |
| Image byte conversion | `Images/ColorSpaceDetailsByteConverter.cs` | `PDColorSpace.toRGBImageAWT` / `ColorConvertOp` |
| Default CMYK | `DeviceCmykColorSpaceDetails` (analytic) | `PDDeviceCMYK` + bundled `CGATS001Compat-v2-micro.icc` |
| Rendering intent | `Graphics/Core/RenderingIntent`, threaded everywhere | `graphics/state/RenderingIntent` (parsed, unused) |

---

## 3. Part A — Output intents

### A1. The output intent is parsed but never consumed — ✅ **Fixed (docs)**

> **Resolution:** the "follow PDFBox" option was taken. No behavioural change; the documentation now describes what the code does. Rewritten: `IResourceStore.OutputIntent` and `GetPageOutputIntent`, `CurrentGraphicsState.OutputIntent`, the `OutputIntentParser` class summary, and `OutputIntent.DestOutputProfile` — each now states plainly that the output intent is exposed for inspection and that device colour spaces convert identically whether or not one is present, noting that PDFBox behaves the same way. Also fixed an unresolvable `<see cref="IccProfileService"/>` on `CurrentGraphicsState` (→ `ParsingOptions.IccProfileService`).
>
> Implementing PDF/X device colour management remains available as a future change; the plumbing that would carry it (`GetPageOutputIntent`, the graphics-state field, the byte cache) is all in place and now honestly documented.

*Original finding:*

`OutputIntent` is stored on `CurrentGraphicsState.OutputIntent` (`BaseStreamProcessor.cs:153`) and exposed via `IResourceStore.OutputIntent` / `GetPageOutputIntent`. A repository-wide grep for consumers returns only the assignment, the `DeepClone`, and one test assertion. `ResourceStore.GetDeviceColorSpaceDetails` — the one place a device colour space could be routed through the output intent profile — does not reference it (`ResourceStore.cs:462-481`).

Meanwhile:

- `IResourceStore.cs:88-90`: *"Used to colour-manage the device colour spaces (DeviceCMYK / DeviceRGB / DeviceGray) per PDF/X semantics."*
- `OutputIntentParser.cs:12-16`: *"rendering those device colours through that profile (rather than a fixed approximation) is what keeps colour-managed content and device-colour content visually consistent."*

Neither is true today.

**PDFBox comparison:** PDFBox is in the same *functional* position — it never uses output intents for rendering either. `PDDeviceCMYK` instead uses a **fixed bundled profile**, `CGATS001Compat-v2-micro.icc`, an open stand-in for "U.S. Web Coated (SWOP) v2" (`PDDeviceCMYK.java:97-114`). So PdfPig is not *behind* PDFBox here; the documentation simply describes an unimplemented feature.

**Options:**
- *Follow PDFBox:* drop the claim from the docs; ship a bundled default CMYK profile in `DeviceCmykColorSpaceDetails` and use the output intent for nothing but inspection/metadata. Lowest risk, matches the reference implementation.
- *Go beyond PDFBox:* wire `ResourceStore.GetDeviceColorSpaceDetails` to build an `ICCBasedColorSpaceDetails`-equivalent from `OutputIntent.DestOutputProfile` when the component count matches. This is what the docs promise and is correct PDF/X behaviour, but it is a deliberate divergence and will change rendered output for every PDF/X file.

Either way, resolve the mismatch before merging — the current state is a documented feature with no implementation.

### A2. The entire output intent is hidden behind `IccProfileService` — ✅ **Fixed**

> **Resolution.** The early `return null` removed; only `TryParseDestOutputProfile` is now gated on the service. The output condition, its identifier, registry name, `/Info`, `/DestOutputProfileRef`, `/MixingHints` and `/SpectralData` are parsed unconditionally — as PDFBox's `getOutputIntents()` exposes them — while `DestOutputProfile` alone stays null without a service. Crucially, skipping the profile also means **not decoding the stream**: an embedded CMYK profile is routinely megabytes and nothing is going to look at it. Two tests assert both halves, and the `IResourceStore` / `CurrentGraphicsState` docs updated to say `null` now means only "the document declares none".

*Original finding:*

`OutputIntentParser.Create` returns `null` immediately when `iccProfileService is null` (`OutputIntentParser.cs:34-37`). Because the default is `null`, **no PdfPig user sees output intent metadata by default** — not `OutputConditionIdentifier`, not `RegistryName`, not `Info`.

**PDFBox:** `PDDocumentCatalog.getOutputIntents()` always returns the list. Colour management is orthogonal.

The metadata has consumers entirely independent of colour management: PDF/A and PDF/X conformance checking, "which press condition was this prepared for", archival tooling. Gating it on a CMM backend is an unnecessary coupling.

**Recommendation:** always parse the dictionary entries; skip only `TryParseDestOutputProfile` when there is no service. This is a small change to `OutputIntentParser.Create` and makes PdfPig strictly more useful than PDFBox here (PDFBox exposes the `/DestOutputProfile` `COSStream` but never parses it).

### A3. One intent with an invented ranking, vs PDFBox's full list — ✅ **Fixed**

> **Resolution.** The list is now the primitive and the policy is public, exactly as recommended.
> - `OutputIntentParser.CreateAll` parses **every** entry, in array order, and `IResourceStore` exposes `OutputIntents` / `GetPageOutputIntents` — the PDFBox-shaped API a conformance check needs, because it needs the entries the policy would discard.
> - The ranking moved out of the parser into **`OutputIntent.SelectEffective`**, a public static whose XML doc states plainly that this is *PdfPig policy, not a rule of ISO 32000*, explains why the question does not arise in a conforming file (PDF/A-1 requires exactly one output intent, PDF/X exactly one PDF/X one), and points a caller who disagrees at the list.
> - The singular `IResourceStore.OutputIntent` / `GetPageOutputIntent` remain as documented conveniences defined as "the list, reduced by that selector", so nothing downstream changed and `CurrentGraphicsState` still carries the single intent in effect.
>
> One behavioural consequence worth stating: the old code sorted first and stopped at the first entry carrying a profile, so it could skip resolving later profiles. `CreateAll` resolves them all. For the normal one-entry document that is identical work, multiple output intents are rare and non-conforming, and the profile cache (**B8**) means repeats cost nothing — but it is a deliberate trade of a little eager work for an honest list.

*Original finding:*

`OutputIntentParser` ranks candidates `GTS_PDFX`(0) > `GTS_PDFA1`(1) > other(2), stable by array order (`GetSubtypeRank`, lines 118-131), then overrides that: any entry with a usable profile beats a better-ranked entry without one (lines 107-112). One `OutputIntent` is returned.

**PDFBox:** returns every entry, unordered and unfiltered. No selection policy at all.

Nothing in ISO 32000-2 14.11.5 establishes this precedence. PDF/A-1 requires exactly one output intent; PDF/X requires exactly one *PDF/X* output intent — so in conforming files the ranking is a no-op, and in non-conforming files PdfPig is guessing. The guess is defensible, but it is policy embedded in a parser, and it is unobservable to callers (there is no way to see the entries that lost).

**Recommendation to follow PDFBox more closely:** expose `IReadOnlyList<OutputIntent>` as the primitive (mirroring `getOutputIntents()`), and keep the ranking as a separate, documented, public "effective output intent" selector on top. Callers doing PDF/A validation need the full list; callers doing colour management need the pick.

### A4. No document-level public API

PDFBox: `document.getDocumentCatalog().getOutputIntents()`. PdfPig: reachable only through `IResourceStore` (an interface a normal caller does not hold) or `CurrentGraphicsState` (only inside a stream processor). `PdfDocument` / `Structure` / catalog expose nothing.

**Recommendation:** surface output intents on the document catalog, as PDFBox does. This is the natural home and the obvious place a user will look.

### A5. Absent required entries become `""` rather than `null` — ✅ **Fixed**

> **Resolution.** `Name`, `OutputConditionIdentifier` and `RegistryName` are now `string?` and stay `null` when the entry is absent, matching `OutputCondition` and `Info` and matching PDFBox's `getString` accessors. The `RegistryName` annotation mismatch is gone with it — the property was already declared `string?` while the constructor parameter was not.
>
> Two tests pin the distinction in both directions: absent entries come back `null`, and an entry the file wrote as empty stays empty rather than being flattened to `null`. That difference is the whole point for a conformance check, since `/S` and `/OutputConditionIdentifier` are required.

*Original finding:*

`OutputIntentParser.cs:62-90` initialises `name = ""`, `outputConditionIdentifier = ""`, `registryName = ""` and leaves them empty when the key is absent. `/S` and `/OutputConditionIdentifier` are **Required** by Table 401, so their absence is diagnostic information a validator wants.

There is also an annotation inconsistency: `OutputIntent.RegistryName` is declared `string?` (`OutputIntent.cs:39`) but the constructor parameter is non-nullable `string registryName` (`OutputIntent.cs:83`) and is never passed null. `OutputCondition` and `Info` correctly use `null`.

**PDFBox:** `COSDictionary.getString` returns `null` for a missing key, uniformly.

**Recommendation:** use `null` for absent entries throughout, matching PDFBox and letting callers distinguish "absent" from "present but empty".

### A6. Page-level output intents — an extension beyond PDFBox

`ResourceStore.GetPageOutputIntent` (`ResourceStore.cs:78-101`) implements PDF 2.0 Table 31 page-level `/OutputIntents` with fallback to the catalog. PDFBox has nothing equivalent. The implementation is sound: the cache is keyed by the `/OutputIntents` array's indirect reference so pages sharing an array share the parse, and the direct-array case correctly re-parses (cheaply, since the profile bytes are cached separately).

Keep it. Just note in the XML docs that this is a PDF 2.0 feature with no PDFBox counterpart, so future porters do not mistake it for a divergence to "fix".

### A7. Write side — ✅ **Fixed**

> **Resolution.** `OutputIntentsFactory` no longer hardcodes `/N 3`; it reads the component count from the profile it is embedding, via a new internal `IccProfileHeader.TryGetNumberOfComponents`. The value is still 3 for the bundled sRGB profile, so nothing written changes today — the point is that it stays correct if that profile is ever swapped or made configurable, and 8.6.5.5 requires `/N` to match the profile.
>
> Note this does put a small ICC header reader in PdfPig, which sits oddly beside **B3**'s conclusion that profile parsing belongs to the backend. The distinction is real: on the *writing* path there is no `IIccProfileService` to ask, and PdfPig owns the profile it ships, so it is the only thing that can get `/N` right. The class documents that scope explicitly.

*Original finding:*

`Writer/Colors/OutputIntentsFactory.cs` emits a `GTS_PDFA1` intent with an embedded sRGB profile, mirroring `PDOutputIntent(PDDocument, InputStream)` + `CreatePDFA`. One small difference: PDFBox sets `/N` from `icc.getNumComponents()` (`PDOutputIntent.java:121`); PdfPig hardcodes `new NumericToken(3)` — correct for the bundled sRGB profile, but brittle if the profile is ever made configurable.

---

## 4. Part B — ICCBased colour spaces

### B1. Colour management is opt-in (PdfPig) vs opt-out (PDFBox)

`ParsingOptions.IccProfileService` defaults to `null` (`ParsingOptions.cs:81`), so `ICCBasedColorSpaceDetails` always falls through to `AlternateColorSpace`.

PDFBox's `PDICCBased` always loads the profile; `useOnlyAlternateColorSpace` is a system-property escape hatch added for LCMS performance (PDFBOX-4309) and explicitly marked as non-conforming.

This is a reasonable platform-driven decision — .NET has no bundled CMM. But it means "PdfPig ported from PDFBox" produces different colours by default for a large class of real files. Two ways to narrow the gap:

- Ship a default `IIccProfileService` in-box (a managed ICC v2/v4 parser handling the common `curv`/`para` + matrix and `mft1`/`mft2`/`mAB ` LUT cases would cover the overwhelming majority of embedded profiles), and default `ParsingOptions.IccProfileService` to it.
- At minimum, document prominently that ICC colour management requires wiring, and that without it PdfPig's ICCBased output is an approximation.

### B2. A wrong `/N` disables colour management instead of being corrected — ✅ **Fixed**

> **Resolution.** PDFBox's behaviour adopted: the profile is believed and `/N` corrected from it, with a warning naming both counts. Three knock-on decisions the original finding anticipated ("more invasive in PdfPig than in PDFBox"):
> - **The `/Alternate` width guard moved from the parser into the constructor.** It has to run *after* the correction — an alternate chosen against a declared `/N` of 3 no longer fits once the profile says 4 — so the constructor is now the single place that knows the final width. The parser still discards an alternate it could not interpret; only the width test moved.
> - **A `/Range` of the wrong length is now ignored rather than thrown.** It was written for the declared `/N`, so a correction makes a mismatch expected rather than exceptional; PDFBox's `getRangeForComponent` likewise falls back to 0..1 instead of refusing the colour space. The old `ArgumentOutOfRangeException` escaped through page parsing and cost the whole page.
> - **A profile claiming a count no ICCBased space may have (not 1, 3 or 4) is rejected** rather than used to "correct" `/N` into something invalid.
>
> 6 tests added covering the correction, the alternate it invalidates, the range fallback, and the invalid-count rejection.

*Original finding:*

```csharp
// ICCBasedColorSpaceDetails.cs:93-100
if (!profileData.IsEmpty && iccService is not null &&
    iccService.TryGetProfile(profileData, out var profile) &&
    profile.NumberOfComponents == NumberOfColorComponents &&
    profile.TryGetTransform(RenderingIntent.RelativeColorimetric, out _))
{
    IccProfile = profile;
}
```

A profile whose component count disagrees with `/N` is silently discarded.

**PDFBox does the opposite** — it trusts the profile and corrects `/N` (PDFBOX-4801):

```java
// PDICCBased.java:341-352
int numIccComponents = iccProfile.getNumComponents();
if (numIccComponents != numberOfComponents) {
    LOG.warn("Using {} components from ICC profile info instead of {} components from /N entry", ...);
    numberOfComponents = numIccComponents;
}
```

So a file with a wrong `/N` renders colour-managed in PDFBox and unmanaged in PdfPig.

**Recommendation:** adopt PDFBox's behaviour. Note this is more invasive in PdfPig than in PDFBox because `NumberOfColorComponents` is set once in the constructor from `numeric.Int` (`ColorSpaceDetailsParser.cs:230`) and also governs operand counts in `GetColor`. The fix is to take the profile's count as authoritative when the two disagree, and log. PDFBox additionally tolerates a *missing* `/N` (`getInt` → `-1`, then corrected from the profile); PdfPig returns `UnsupportedColorSpaceDetails` (`ColorSpaceDetailsParser.cs:229-233`).

### B3. No sRGB fast path — ⛔ **Not PdfPig's to fix; belongs to the ICC backend**

> **Reclassified.** This was filed against PdfPig, and that was wrong. Both halves of the optimisation sit entirely behind `IIccProfileService`:
>
> - **Detecting sRGB** means reading the profile header's device-model field — profile parsing, which is by definition the backend's job. PdfPig deliberately does not parse ICC bytes at all.
> - **Short-circuiting** needs no PdfPig involvement either: a backend that recognises an sRGB profile can return an `IIccTransform` whose `ToRgb` returns its inputs and whose `Transform` copies bytes through. That captures essentially the whole win. What would remain on PdfPig's side — one virtual call and a clip loop over three doubles — is noise next to a CMM lookup, which is what the fast path exists to avoid.
>
> PDFBox has to do this inside `PDICCBased` only because it has no backend abstraction to put it behind: `PDICCBased` *is* the glue over the platform CMM, so substituting the JVM's built-in sRGB profile is something only it can do. PdfPig's provider split means the equivalent belongs one layer down, exactly as the display-class fixup does (**B12**).
>
> Adding `bool IsSRgb` to `IIccProfile`, as originally recommended, would therefore have been the wrong shape: it puts a profile-parsing fact on the interface and then obliges every colour-space entry point to branch on it, to buy back something the backend could have handled without telling PdfPig anything. **No PdfPig change is warranted.** The `IIccProfileService` contract now names it alongside the other things implementations own.

*Original finding:*

PDFBox checks the profile header's device-model field for `"sRGB"` (`PDICCBased.java:246-252`), and if it matches:

- substitutes the JVM's built-in sRGB profile (PDFBOX-2587, *"a large performance gain as it's our native color space"*),
- sets `isRGB`, which makes `toRGB(value)` **return the input unchanged** (`PDICCBased.java:283`),
- enables `PDIndexed.toRawImage`'s zero-conversion path.

Most embedded RGB profiles in the wild are sRGB. PdfPig has no equivalent: every sRGB ICCBased fill, stroke and image goes through the full transform.

**Recommendation** *(superseded — see above)***:** add `bool IsSRgb { get; }` to `IIccProfile` (or detect it in PdfPig from the header before calling the service, which keeps every backend honest), and short-circuit `Process` / `GetColor` / `GetRgb` / `Transform` to identity.

### B4. `/Alternate` arrays are silently ignored — ✅ **Fixed**

> **Resolution:** `/Alternate` now routes through `GetSecondaryColorSpace`, which handles the name form, the array form, and indirect references to either. Three supporting decisions:
> - `GetSecondaryColorSpace` gained an `applyDefaultSubstitution` parameter (default `true`). The `/Alternate` call passes `false`, so `DefaultGray`/`DefaultRGB`/`DefaultCMYK` do not capture it — matching PDFBox, which resolves the alternate with no resources at all. The 8.6.5.6 cases (Indexed base, Separation/DeviceN alternate, Pattern underlying space) are unaffected.
> - An alternate that resolves to `UnsupportedColorSpaceDetails` is discarded rather than adopted, so the space falls back to the device space implied by `/N`. This covers a Pattern alternate, which Table 66 forbids, without a special case.
> - **An alternate whose component count disagrees with `/N` is rejected.** This guard is required, not optional: the alternate is handed the very same operands as the profile, so a 3-component alternate under `/N 4` made `GetColor` throw on the operand count. Enabling array alternates without it would have turned a silently-ignored entry into a crash.
>
> 11 tests added in `UglyToad.PdfPig.Tests/ContentTests/IccBasedAlternateColorSpaceTests.cs`, covering all four `/Alternate` shapes, the width guard, Pattern and unparseable alternates, the `/N`-implied default, and both directions of the default-substitution scoping.

*Original finding:*

```csharp
// ColorSpaceDetailsParser.cs:236-242
if (streamToken.StreamDictionary.TryGet(NameToken.Alternate, out NameToken alternateColorSpaceNameToken) &&
    ColorSpaceMapper.TryMap(alternateColorSpaceNameToken, resourceStore, out var alternateColorSpace))
```

Only a direct `NameToken` is handled. Note also the missing `scanner` argument, so an *indirect* `/Alternate` name is missed too.

**PDFBox handles both shapes** (`PDICCBased.java:391-431`): `COSName` is wrapped into a one-element array; a `COSArray` is used as-is; anything else throws.

So `/Alternate [/ICCBased 12 0 R]`, `/Alternate [/Separation /Spot /DeviceCMYK 14 0 R]`, `/Alternate [/CalRGB <<...>>]` — all legal — are silently dropped by PdfPig, which then falls back to the device space implied by `/N`. Without an `IIccProfileService` configured (the default!) this is the *only* thing determining the rendered colour, so the impact is not theoretical.

**Recommendation:** route `/Alternate` through the existing `GetSecondaryColorSpace` helper (`ColorSpaceDetailsParser.cs:485`), which already handles both the name and array forms and resolves through the scanner. Highest correctness-per-line-changed fix in this review.

*(One thing already at parity: PDFBox calls `PDColorSpace.create(alternateArray)` with no `resources`, so `/Alternate` gets no `DefaultGray`/`DefaultRGB`/`DefaultCMYK` substitution. PdfPig's path via `ColorSpaceDetailsParser` likewise bypasses `resourceStore.GetDeviceColorSpaceDetails`. Correct — preserve this if the above is changed, since `GetSecondaryColorSpace` **does** apply the substitution at lines 499-502.)*

### B5. Array-length strictness — ✅ **Fixed**

> **Resolution.** The ICCBased array check relaxed from `Length != 2` to `Length < 2`, matching PDFBox's `checkArray`. Only the first two elements are read, so a trailing junk element no longer costs the page its colours. Scoped to ICCBased; CalGray/CalRGB/Lab keep their exact-length checks, as the original finding suggested.

*Original finding:*

PdfPig requires `colorSpaceArray.Length != 2` → `UnsupportedColorSpaceDetails` (`ColorSpaceDetailsParser.cs:212-216`). PDFBox requires `size() >= 2` and ignores extras (`PDICCBased.checkArray`).

An ICCBased array with a trailing junk element renders in PDFBox and is dropped by PdfPig. Cheap fix; relax to `>= 2`.

*(This exact-length pattern is repeated for CalGray, CalRGB and Lab in the same file. Same reasoning applies, though those are less commonly malformed.)*

### B6. No logging anywhere on the ICC path — ✅ **Fixed**

> **Resolution.** `ILog` threaded through the whole ICC path, surfaced as a new `IResourceStore.Logger` (fed from `ParsingOptions.Logger`, consistent with the `IccProfileService` / `OutputIntent` members this branch already added to that interface). Every degradation now says so:
> - `/N` corrected from the profile, and a profile whose component count is not 1/3/4;
> - a profile that resolved but could not convert a colour;
> - an `/Alternate` that could not be interpreted, and one of the wrong width;
> - a `/Range` of the wrong length;
> - an `IIccProfileService` that declined a profile, and one that *threw* — the latter previously escaped as an exception and is now caught, logged, and treated as a decline.
>
> `RecordingLog` in the tests asserts the messages actually reach the log rather than merely that the fallback happened.

*Original finding:*

PDFBox logs a warning at every degradation point: profile load failure with the exception message and the chosen alternate (`PDICCBased.java:236-237`), `/N` correction (`:347`), display-class rewrite (`:264`).

PdfPig is silent throughout:

- `IccProfileByteCache.Decode` catches *everything* and returns empty (`IccProfileByteCache.cs:83-91`),
- the constructor drops mismatched or untransformable profiles with no trace,
- `GetTransformWithFallback` silently downgrades an unsupported intent to RelativeColorimetric,
- `OutputIntentParser.TryParseDestOutputProfile` returns `null` for several distinct failure modes.

`ParsingOptions.Logger` exists and is used elsewhere in the codebase (`ResourceStore.cs:549`, `BaseStreamProcessor`). "My PDF/X file isn't colour-managed and I can't tell why" is currently undiagnosable.

**Recommendation:** thread the logger into `ColorSpaceDetailsParser`, `ICCBasedColorSpaceDetails`, `OutputIntentParser` and `IccProfileByteCache`, and log at each fallback with the reason. Directly mirrors PDFBox.

### B7. The validation "smoke test" is weaker than PDFBox's — ✅ **Fixed**

> **Resolution:** both halves of the recommendation were implemented, because neither alone is sufficient.
> - **Validation now converts.** `IsUsable(profile, components)` replaces the bare `TryGetTransform` check: it obtains the RelativeColorimetric transform and then exercises *both* conversion entry points — `ToRgb` with a zero vector (in range for every data colour space, Lab included) and `Transform` with a one-pixel buffer — inside a try that returns `false`. The two are separate implementations that fail separately, which is the direct analogue of PDFBox calling `toRGB` *and* constructing a `ComponentColorModel` inside the same try.
> - **Conversion sites are guarded.** Validation can only prove the profile converts the input it probed. A `volatile bool iccTransformFailed` latch, set by a shared `TryToRgb` helper and by the byte path's own catch, retires the profile permanently on the first failure; `GetTransformWithFallback` then returns `null` and every path falls through to `AlternateColorSpace`. The latch is per colour space rather than per entry point, so a scalar failure also stops the image path from hitting the same defect, and a broken profile costs one exception per document rather than one per pixel.
>
> The byte path's fallback is safe because `IIccTransform.Transform` takes a `ReadOnlySpan` source, so a partially completed conversion cannot have consumed the samples the alternate then needs.
>
> 6 tests added to `ICCBasedColorSpaceDetailsTests`: rejection at construction on either entry point, graceful fallback when a validated profile starts throwing, the latch not retrying, the byte path falling back with its source intact, and the latch crossing between entry points.

*Original finding:*

PDFBox deliberately forces the lazily-initialised CMM to fail *at load time*, inside the try block:

```java
// PDICCBased.java:208-216 — comments abridged
awtColorSpace.toRGB(new float[numOfComponents]);          // triggers CMMException / ProfileDataException / AIOOBE
new ComponentColorModel(awtColorSpace, false, false, ...); // PDFBOX-4015: triggers "LCMS error 13"
```

with a catch clause covering `ProfileDataException | CMMException | IllegalArgumentException | ArrayIndexOutOfBoundsException | IOException`. Four separate JIRAs are cited for cases where the failure only surfaced on first use.

PdfPig's analogue is `profile.TryGetTransform(RenderingIntent.RelativeColorimetric, out _)` — it obtains a transform handle but **never performs a conversion**. A backend that builds its LUT pipeline lazily can still throw later, from `Process` / `GetColor` / `GetRgb` / `Transform`, none of which have a try/catch. That exception escapes through `page.GetImages()` or the stream processor.

**Recommendation:** either (a) run one actual conversion at construction — `t.ToRgb(zeroes)` — so failures are caught where the alternate fallback lives, or (b) guard the conversion call sites so a late backend failure degrades to `AlternateColorSpace` rather than propagating. PDFBox's history says (a) is not paranoia.

### B8. Parsed profiles are not cached across resource dictionaries — ✅ **Fixed**

> **Resolution.** The existing cache was extended rather than replaced, as the original finding recommended, and its two dictionaries now key the parsed profile the same two ways the bytes were keyed — so `IIccProfileService.TryGetProfile` is called **at most once per profile per document**, the guarantee PDFBox gets from caching the constructed `PDICCBased` against the stream's `COSObject`. The content hash that identifies a directly written profile is computed once per lookup.
> - `ICCBasedColorSpaceDetails` now takes a resolved `IIccProfile?` instead of `(bytes, service)`, so the colour space no longer parses anything; resolution belongs to the cache.
> - "Parsed, and the answer was no profile" is remembered as firmly as a success, so a profile the service declines is not retried per page.
> - `OutputIntentParser` shares the same path, which matters because a PDF/X file routinely points its `/DestOutputProfile` and an `/ICCBased` colour space at the same stream object.
> - `Jpeg2000Helper` resolves directly and is documented as the deliberate exception: a JPX profile is a slice of the image's own codestream, not a stream object the document can point at twice, so there is no key to share it under.
>
> **Follow-up, now done.** The first cut kept the decoded bytes alongside the parsed profile, which meant a multi-megabyte inflated CMYK profile stayed alive for the document's lifetime after the only thing that needed it had finished. Since `GetOrDecode` turned out to have no production callers at all, it was removed and `GetOrParse` became the sole entry point: the bytes are now a local inside `DecodeAndParse`, collectable the moment the service has built its profile, and the dictionaries hold `IIccProfile?` directly. The class was renamed `IccProfileByteCache` → **`IccProfileCache`**, since bytes are the one thing it no longer caches (internal type; no public API impact).
>
> That this is not tested is deliberate and noted in the test file: with no field left that could hold the bytes, the guarantee is structural, and asserting it would take a GC-and-weak-reference test — the flakiest kind there is — to re-prove something the type already gives.

*Original finding:*

**PDFBox:** `PDICCBased.create` caches the *constructed colour space object* in the document `ResourceCache`, keyed by the ICC array's `COSObject` (`PDICCBased.java:108-132`). The parsed `ICC_Profile` and its CMM transforms are shared for the document's lifetime.

**PdfPig:** two caches, neither of which does this.
- `IccProfileByteCache` caches **decoded bytes** document-wide (good, and well documented).
- `ResourceStore.loadedColorSpaceDetailsCache` caches `ColorSpaceDetails`, but is **cleared on every `LoadResourceDictionary` and `UnloadResourceDictionary`** (`ResourceStore.cs:106-107`, `243-244`).

So each resource dictionary — every page, and every Form XObject with its own `/Resources` — rebuilds `ICCBasedColorSpaceDetails` and calls `IIccProfileService.TryGetProfile` again. The interface doc pushes the fix onto implementers: *"Implementations should cache parsed profiles (recommended key: profile content hash)"* (`IIccProfileService.cs:12-13`). That means every backend must independently implement a correctness-relevant cache, and the default path pays a content hash of a multi-megabyte profile per page.

This also explains the `XObjectFactory.Resolve` workaround (`XObjectFactory.cs:22-38`), which avoids resolving `/ColorSpace` purely so the indirect reference survives as a usable cache key — machinery PDFBox does not need, because it caches on the `COSObject` itself.

**Recommendation:** extend `IccProfileByteCache` (or add a sibling) to memoise the parsed `IIccProfile` under the same indirect-reference / content key it already computes. One call to `TryGetProfile` per profile per document, matching PDFBox's guarantee, and `IIccProfileService` implementations become genuinely stateless.

### B9. No `GetDefaultDecode` — image Decode defaults are wrong for non-`[0,1]` spaces — ✅ **Fixed**

> **Resolution.** Both recommendations implemented, plus the byte-encoding contract they imply.
> - **`ColorSpaceDetails.GetDefaultDecode(int bitsPerComponent, Span<double> destination)`** added as `public virtual` (PDFBox makes it abstract; virtual here keeps external subclasses compiling and means only the spaces that differ have to say so). Overridden by `IndexedColorSpaceDetails` (`[0, 2^bpc - 1]`), `LabColorSpaceDetails` (`[0 100 amin amax bmin bmax]`) and `ICCBasedColorSpaceDetails` (profile ranges, else the alternate's — mirroring `PDICCBased.getDefaultDecode`).
> - **`IIccProfile.IsLabInput` replaced by `IReadOnlyList<double> ComponentRanges`**, the counterpart of `ICC_ColorSpace.getMinValue`/`getMaxValue`. The hardcoded `LabRange` constant is gone; a profile declaring anything other than `[0, 1]` now overrides `/Range`, which generalises the old Lab special case without changing it. Snapshotted once at construction, so there is no per-pixel virtual dispatch.
> - **`ColorSpaceDetailsByteConverter.ApplyDecode` reworked** to take its defaults from the colour space and, for non-Indexed spaces, to store the component's *position* within that range rather than its value. This is the piece that makes the byte pipeline coherent: a byte cannot hold an L\* of 100 or a negative a\*, and `DecodeRawComponents` reverses the mapping in `Transform`, so the pair round-trips. For a `[0, 1]` space position and value coincide and the behaviour is bit-for-bit what it was. For an L\*a\*b\* ICC profile it coincides with the ICC.1 8-bit encoding, now documented on `IIccTransform.Transform`.
> - **`LabColorSpaceDetails.Transform` fixed** to decode via `DecodeRawComponents` instead of `/255`. It was handing `GetRgb` an L\* of at most 1 (of 100), so **every Lab image rendered near-black**; that is fixed as a direct consequence.
> - Degenerate ranges (`/Range [0 0 0 0]`, which is legal) resolve to position zero rather than dividing by zero.
>
> 13 tests added in `UglyToad.PdfPig.Tests/Images/DefaultDecodeTests.cs`, including the scalar-vs-image round-trip the section below called out as missing, and 5 more in `ICCBasedColorSpaceDetailsTests`.

*Original finding:*

PDFBox makes `getDefaultDecode(int bitsPerComponent)` **abstract on `PDColorSpace`**, and `PDICCBased` returns the ICC colour space's actual per-component bounds:

```java
// PDICCBased.java:358-375
decode[i * 2]     = awtColorSpace.getMinValue(i);
decode[i * 2 + 1] = awtColorSpace.getMaxValue(i);
```

For a Lab-input profile that is L*∈[0,100], a*,b*∈[-128,127].

PdfPig has no `GetDefaultDecode` at all. `ColorSpaceDetailsByteConverter.ApplyDecode` hardcodes `defaultDMax = 1.0` for every non-Indexed space (`ColorSpaceDetailsByteConverter.cs:100`) and scales to bytes.

The two PdfPig code paths then disagree with each other:
- **Scalar path** (`GetColor` / `GetRgb`): `ClipForProfile` clips to a hardcoded `LabRange` constant when `IsLabInput` (`ICCBasedColorSpaceDetails.cs:152-165`).
- **Image path** (`Transform`, lines 252-267): hands raw bytes straight to `IIccTransform.Transform` with **no `Range` clip and no Lab handling at all**.

The same colour reached as a fill and as an image sample converts differently.

**Recommendations:**
1. Add `GetDefaultDecode(int bitsPerComponent)` to `ColorSpaceDetails`, mirroring PDFBox's abstract method, and have `ColorSpaceDetailsByteConverter` use it instead of the `[0,1]` assumption.
2. Replace `IIccProfile.IsLabInput` (a bool) with per-component `MinValue`/`MaxValue` accessors — PDFBox gets these free from `ICC_ColorSpace.getMinValue/getMaxValue`, and the hardcoded `LabRange` array becomes unnecessary. This also fixes the case of a profile whose data space is neither `[0,1]` nor Lab.

### B10. Indexed base with a Lab-input ICC profile decodes to near-black — ✅ **Fixed**

> **Resolution.** `ICCBasedColorSpaceDetails` now overrides `DecodeRawComponents`, decoding colour-table bytes through the profile's own `ComponentRanges` (falling back to the alternate colour space when there is no profile, since the alternate owns the conversion that follows). An Indexed palette over an L\*a\*b\* profile reaches the transform as L\* in [0, 100]. This closes the gap noted in the original finding: PdfPig had already chosen to be better than PDFBox for a direct Lab base, and an ICCBased base did not inherit it.

*Original finding:*

`ICCBasedColorSpaceDetails` does not override `DecodeRawComponents`, so an Indexed colour table over an ICCBased base uses the `ColorSpaceDetails` default of `byte / 255.0` → `[0,1]`. `ClipForProfile` then clips into `LabRange` (a no-op for values already in `[0,1]`), so L* arrives as ≈0-1 and renders black.

This is the exact bug the branch deliberately fixed for `LabColorSpaceDetails` — see the comment at `IndexedColorSpaceDetails.cs:81-93`: *"feeding Lab a [0, 1] L* renders near-black."*

**PDFBox has the identical bug** — `PDIndexed.readColorTable` is a flat `(lookupData[offset] & 0xff) / 255f` regardless of base (`PDIndexed.java`). So this is *at parity* with PDFBox, but PdfPig has already chosen to be better here for the direct Lab case and should finish the job. Fixed by the same change as **B9**.

### B11. `BaseNumberOfColorComponents` when falling back to the alternate — ✅ **Fixed**

> **Resolution.** Changed to `AlternateColorSpace.BaseNumberOfColorComponents`. As the original finding predicted, this stopped being merely latent the moment **B4** made array alternates load: `/N 1` with `[/Separation /Spot /DeviceCMYK <tint>]` reports 4, which is what `Transform` actually emits and what `PngFromPdfImageFactory` sizes its buffer from. Tests cover both the new case and the device-alternate case that was already right.

*Original finding:*

```csharp
// ICCBasedColorSpaceDetails.cs:117-120
BaseType = AlternateColorSpace.BaseType;
BaseNumberOfColorComponents = NumberOfColorComponents;
```

`BaseType` comes from the alternate but the component count comes from `/N`. These disagree when the alternate's base has a different width — e.g. `/N 1` with `/Alternate [/Separation /Spot /DeviceCMYK f]`: `BaseType` is `DeviceCMYK` but `BaseNumberOfColorComponents` is 1, while `Transform` delegates to the alternate and emits 4 bytes/pixel. `PngFromPdfImageFactory` reads `BaseNumberOfColorComponents` to interpret that buffer (`PngFromPdfImageFactory.cs:91`).

This is **pre-existing** (`HEAD~1` had `BaseNumberOfColorComponents => NumberOfColorComponents`), not a regression, and it is currently unreachable because **B4** means array `/Alternate` values never load. It becomes reachable the moment B4 is fixed. There is no PDFBox counterpart — `BaseNumberOfColorComponents` is a PdfPig concept — so the fix is simply `AlternateColorSpace.BaseNumberOfColorComponents`.

### B12. No display-class fixup — ✅ **Resolved (decided and documented)**

> **Resolution.** Decided in favour of *backend responsibility*, and written into the `IIccProfileService` contract. PdfPig has no colour engine of its own, so patching profile bytes for an engine that may not object would be speculative; instead the interface now states plainly that tolerating malformed profiles is the implementation's job, cites PDFBOX-4114 and the display-class rewrite as the known case, and spells out what PdfPig does when an implementation declines (drop the profile, use the alternate) and the one thing it must not do (report success and then produce wrong colours). The original finding's complaint was that this was *neither* implemented nor documented; it is now unambiguous.

*Original finding:*

PDFBox rewrites the device class of a non-display, Perceptual-intent profile so the CMM will accept it (`ensureDisplayProfile`, `PDICCBased.java:256-270`, PDFBOX-4114, borrowed from TwelveMonkeys). PdfPig delegates entirely to the backend and simply drops the profile if `TryGetTransform` fails.

**Recommendation:** either perform the same 4-byte header patch in PdfPig before handing bytes to the service — so every backend benefits and behaviour is uniform — or document it explicitly as a backend responsibility in the `IIccProfileService` contract. Currently it is neither, so behaviour silently varies by backend.

### B13. JPX — at parity, with a note

`Jpeg2000Helper.GetJpxColorSpaceDetails` now feeds the JP2 ICC box into `ICCBasedColorSpaceDetails` (`Jpeg2000Helper.cs:112-119`) rather than discarding it. PDFBox wraps whatever `ColorSpace` the JAI reader produced in `PDJPXColorSpace`. Both only consult the embedded colour space when the image dictionary has no `/ColorSpace`. Functionally equivalent; PdfPig's is arguably cleaner since it does not depend on an image-codec side effect.

Note `PDJPXColorSpace.getDefaultDecode` returns the AWT space's real min/max — the same point as **B9**.

### B14. No `toRawImage` equivalent

PDFBox's `PDColorSpace.toRawImage` returns the image in its native colour space without conversion where possible; `PDIndexed.toRawImage` uses it to build an `IndexColorModel` directly when the base is an sRGB `PDICCBased` (`PDIndexed.java:257-277`). PdfPig always converts to RGB. Minor — and note a backend implementing the **B3** fast path already removes most of what this would save, since the conversion it avoids becomes a copy.

---

## 5. Ranked recommendations

### Fix before merge — ✅ all three done
| # | Item | Why | Status |
|---|---|---|---|
| **A1** | Resolve the output-intent doc/behaviour mismatch | Documents a feature that does not exist | ✅ Docs corrected; no behavioural change |
| **B4** | Accept array-valued and indirect `/Alternate` | Silent wrong colours on legal files; affects the *default* no-CMM path | ✅ Fixed, + component-width guard |
| **B7** | Make profile validation actually convert a colour, or guard conversion sites | Backend exception can escape `page.GetImages()` | ✅ Both: probing validation + failure latch |

### High value, follows PDFBox closely — ✅ four of five done, 1 marked as out of scope
| # | Item | PDFBox reference | Status |
|---|---|---|---|
| **B2** | Correct `/N` from the profile instead of dropping the profile | `PDICCBased.java:341-352` (PDFBOX-4801) | ✅ Fixed, + the alternate/range consequences |
| **B6** | Log every ICC/output-intent fallback via `ParsingOptions.Logger` | `PDICCBased.java:236, 264, 347` | ✅ Fixed via new `IResourceStore.Logger` |
| **B8** | Cache the parsed `IIccProfile` document-wide, not just its bytes | `PDICCBased.create` + `ResourceCache` | ✅ Fixed; one parse per profile per document |
| **A2** | Parse output intent metadata without requiring an `IIccProfileService` | `PDDocumentCatalog.getOutputIntents()` | ✅ Fixed; the stream is not even decoded |
| **B3** | sRGB detection → identity fast path | `PDICCBased.is_sRGB`, PDFBOX-2587 | ⛔ Not PdfPig's — belongs to the `IIccProfileService` implementation |

### Correctness, medium priority — ✅ all five done
| # | Item | Status |
|---|---|---|
| **B9** | Add `ColorSpaceDetails.GetDefaultDecode`; replace `IsLabInput` with per-component min/max on `IIccProfile` | ✅ Both, + the byte-encoding contract they imply; fixed Lab images rendering near-black |
| **B10** | Override `DecodeRawComponents` on `ICCBasedColorSpaceDetails` (falls out of B9) | ✅ Fixed |
| **B5** | Relax ICCBased array length to `>= 2` | ✅ Fixed |
| **B12** | Decide and document where the display-class fixup lives | ✅ Decided: backend responsibility, written into the `IIccProfileService` contract |
| **B11** | `BaseNumberOfColorComponents` → `AlternateColorSpace.BaseNumberOfColorComponents` | ✅ Fixed (became live once B4 landed) |

### API shape — ✅ three of five done, 2 marked as won't do
| # | Item | Status |
|---|---|---|
| **A3** | Expose the full output-intent list; make the ranking a separate documented selector | ✅ `OutputIntents` / `GetPageOutputIntents` + public `OutputIntent.SelectEffective` |
| **A5** | `null` rather than `""` for absent entries; fix the `RegistryName` nullability mismatch | ✅ Fixed |
| **A7** | Derive `/N` from the profile in `OutputIntentsFactory` rather than hardcoding 3 | ✅ Fixed via internal `IccProfileHeader` |
| **A4** | Surface output intents on the document catalog | ⛔ Won't do |
| **B1** | Ship a default `IIccProfileService`, or document the opt-in gap prominently | ⛔ Won't do |

---

## 6. Divergences worth keeping

These are places where PdfPig deliberately does *not* follow PDFBox, and should not be "corrected":

- **Rendering intent threaded through the colour pipeline.** PDFBox parses it and throws it away. PdfPig honours it, with a documented RelativeColorimetric fallback (`GetTransformWithFallback`) matching ISO 32000-2 8.6.5.8. Cost: `intent` is an ignored parameter on every non-ICC colour space. Worth it.
- **The `IIccProfileService` / `IIccProfile` / `IIccTransform` abstraction.** There is no PDFBox analogue because the JDK supplies the CMM. The three-level split (service → profile → intent-bound transform) is the right shape and correctly documents its thread-safety requirements.
- **Page-level `/OutputIntents`** (PDF 2.0 Table 31). No PDFBox support at all.
- **`isResolvingDefaultSubstitute`** mirrors PDFBox's `wasDefault` flag exactly and is credited in the comment (`ResourceStore.cs:45-50`) — good porting hygiene. PdfPig additionally rejects Lab/Indexed/Pattern as `Default*` substitutes per 8.6.5.6, which PDFBox does not check. Correct divergence.
- **`IccProfileCache`'s dual keying** (indirect reference / MurmurHash3 content key). PDFBox does not need this because it caches at a different level, but given PdfPig's token model the design is sound and the rationale is unusually well documented. If **B8** is implemented, this cache should be extended rather than replaced.
- **`ColorSpaceDetails.DecodeRawComponents`** for Indexed colour tables — strictly more correct than PDFBox's flat `/255`, and since **B10** it covers an ICCBased base too, which PDFBox does not.
- **The sample-byte encoding** introduced by **B9**: a byte holds the component's position within its default-decode range, not its value. PDFBox has no equivalent because AWT rasters carry their own colour model; PdfPig's `Span<byte>` pipeline needs an explicit convention, and this is the one that lets Lab and L\*a\*b\* ICC profiles share it with the device spaces unchanged.

---

## 7. Test coverage observations

`OutputIntentParserTests.cs` covers the ranking policy thoroughly — including the "profile availability outranks subtype" rule and the "must not merely reverse the array" case — and now also the full-list API, the null-vs-empty distinction, and non-dictionary array entries. `IccProfileCacheTests.cs` covers both keying strategies and the parse-once guarantee. `ResourceStorePageOutputIntentTests.cs` covers page-vs-catalog resolution for both the list and the singular convenience. `OutputIntentsFactoryTests.cs` covers the write side and the ICC header reader.

Gaps, aligned with the findings above:

- ~~No test for an ICCBased colour space whose `/Alternate` is an array~~ — ✅ closed by `IccBasedAlternateColorSpaceTests` (11 tests).
- ~~No test that an `IIccProfileService` throwing from `ToRgb`/`Transform` degrades gracefully~~ — ✅ closed by 6 tests in `ICCBasedColorSpaceDetailsTests`.
- No test asserts that the output intent is *used* — correct, and now documented as intentional (**A1**).
- ~~No round-trip test comparing the scalar path (`GetColor`) against the image path (`Transform`) for the same colour~~ — ✅ closed by `DefaultDecodeTests.LabImage_ImagePathAgreesWithTheScalarPath`, along with 12 more default-decode / byte-encoding tests.
- ~~No test for `/N` disagreeing with the profile~~ — ✅ closed, along with tests for the profile cache's parse-once guarantee and for the log messages themselves.
- `IccProfileBenchmarks` covers the caching work well (three GWG PDF/X files plus the 37-ICC-image `iron-ore` case). A benchmark with an sRGB profile would still be worth having, but it would measure a backend's fast path (**B3**) rather than anything PdfPig controls.
