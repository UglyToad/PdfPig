# Code review — PdfPig `4999954d`

> "Add support for ICC profile parsing, caching and conversion, output intent parsing,
> and rendering intent aware color conversion"

**Scope reviewed:** production code under `src/UglyToad.PdfPig` and `src/UglyToad.PdfPig.Tokens`
(39 files). Tests, benchmarks and `PdfPig.Rendering.Skia` were read for context only, not reviewed.

**Status:** **#1, #2, #3, #4, #6, #8, #9, #10 and #14 are fixed. #5, #15 and #16 are won't-do.**

| Finding | State |
|---|---|
| #1 unbounded `/Alternate` recursion | **Fixed** — `allowIccBased` flag, 2 regression tests (`f08c84e4`) |
| #2 `GetInitializeColor` clips one component | **Fixed** — per-component loop, 2 regression tests (`f08c84e4`) |
| #3 uncoloured tiling pattern skips output intent | **Fixed** — profile threaded to `ForPattern`, 4 tests (`56413c9a`) |
| #4 image path leaks the DeviceGray expansion | **Fixed** — transform wrapped to the image's width, 5 tests (working tree) |
| #5 undeclared public API breaks | **Won't do** |
| #6 `CurrentStrokingColor` nullability | **Fixed** — now `IColor?` (`56413c9a`) |
| #8 page-level intents lost by nested processors | **Fixed** — second constructor, 5 tests (working tree) |
| #9 `IccProfileReference` span properties | **Fixed** — now `ReadOnlyMemory<byte>` (`56413c9a`) |
| #10 output intent re-selected per operator | **Fixed** — the state carries the profile, 6 tests (working tree) |
| #14 unclamped third-party transform output | **Fixed** — clipped at the ICC boundary, 15 tests (working tree) |
| #15 unsynchronised `ResourceStore` state | **Won't do** |
| #16 line-ending churn in the diff | **Won't do** |
| #17 documentation defects | Mostly fixed in the working tree — see the finding |
| #7, #11, #12, #13, #18, #19 | Open |

Committed through `478d5cd7`; #10, #14 and the #17 doc fixes are in the working tree, together with
the matching changes to `PdfPig.Rendering.Skia`.

> **A note on how the remaining fixes were shaped.** The first attempts at #4 and #8 were designed so
> that existing consumer code kept compiling. That was the wrong instinct: this ICC layer is
> unreleased and `PdfPig.Rendering.Skia` is its only consumer, so there is no compatibility to
> preserve and back-compat shaping only bought a worse design. Both were redone for the best shape,
> and Skia was updated to match. The discarded versions are recorded in each finding, because the
> reasoning is the point.

**Verification performed**

| Command | Result |
|---|---|
| `dotnet build src/UglyToad.PdfPig -c Release` (all 7 TFMs, `-t:Rebuild`) | 0 errors; no new warnings in the touched files |
| `dotnet test src/UglyToad.PdfPig.Tests -c Release -f net8.0` | **4482 passed, 7 skipped, 0 failed**, with every fix applied |
| Same, at commit `4999954d` before any fix | 4446 passed, 7 skipped, 0 failed — the +36 are the new regression tests |
| `dotnet test UglyToad.PdfPig.Rendering.Skia.Tests -c Release` | **538 passed, 0 failed** on each of net462 / net6.0 / net8.0 / net9.0 |

Real diff size of the reviewed commit, once line-ending churn is discounted
(`git diff --ignore-cr-at-eol`): ~1,900 added / ~250 removed lines of production code (the raw stat
says 7,757 / 560). The fixes add a further 103 insertions / 4 deletions (#1, #2), 220 / 19
(#3, #6, #9), 321 / 56 (#4, #8) and a further round for #10 and #14 — each including the matching
`PdfPig.Rendering.Skia` changes.

---

## Summary

This is a well-structured, unusually well-documented change. The central design decision — putting
the colour engine behind `IIccProfileService` / `IIccProfile` / `IIccTransform` rather than taking a
dependency — is the right call: it keeps the core dependency-free, netstandard2.0-compatible and
AOT-friendly, and it lets a malformed profile degrade to the alternate colour space instead of
failing the page. Several details are handled better than most PDF libraries manage:

- The profile, not `/N`, is treated as the authority on component count, with the disagreement
  logged and the alternate re-validated afterwards (`ICCBasedColorSpaceDetails` ctor).
- `IsUsable` probes **both** conversion entry points before committing to a profile, because a
  profile that parses can still throw on use.
- The `GetDefaultDecode` / `DecodeRawComponents` pairing finally makes Lab and Lab-ICC image samples
  round-trip correctly, which the old hard-coded `/255.0` could not.
- Failure is never held against a profile permanently: a colour that fails converts through the
  alternate and the next colour tries the profile again.

The findings below are ordered by severity. Nothing here blocked the feature itself; #1 was the only
one I would have insisted on before release, and it is now fixed — as are the two other outright
bugs, #2 and #3, plus the two API-shape findings #6 and #9, the two design gaps #4 and #8, and the
performance and robustness items #10 and #14. #5, #15 and #16 have been accepted as won't-do. What
remains is #7 and a handful of tidy-ups.

---

## Correctness

### 1. `/Alternate` array on an ICCBased space can recurse without bound → StackOverflow — **FIXED**

**File:** `src/UglyToad.PdfPig/Util/ColorSpaceDetailsParser.cs:211-350`

Before this commit `/Alternate` was only read as a `NameToken`:

```csharp
if (streamToken.StreamDictionary.TryGet(NameToken.Alternate, out NameToken alternateColorSpaceNameToken) && ...)
```

It is now resolved through `GetSecondaryColorSpace`, which accepts an **array** form. But
`case ColorSpace.ICCBased:` never consults the `cannotRecurse` flag (unlike `Indexed` and `Pattern`,
which both bail out on it), and although `GetSecondaryColorSpace` passes `cannotRecurse: true`, the
ICCBased arm simply ignores it.

So a file containing

```
5 0 obj [/ICCBased 6 0 R] endobj
6 0 obj << /N 3 /Alternate 5 0 R /Length ... >> stream ... endstream endobj
```

recurses `GetColorSpaceDetails → GetSecondaryColorSpace → GetColorSpaceDetails → …` until the stack
is exhausted. `StackOverflowException` cannot be caught in .NET — it kills the process — so this is
reachable from untrusted input and not containable by a host application's try/catch.

ISO 32000-2 8.6.5.5 says the `/Alternate` of an ICCBased space **shall not** be another ICCBased
space, so the fix is cheap and spec-aligned — either honour `cannotRecurse` in the ICCBased arm, or
reject an ICCBased alternate explicitly:

```csharp
var alternate = GetSecondaryColorSpace(..., applyDefaultSubstitution: false);

if (alternate is ICCBasedColorSpaceDetails or UnsupportedColorSpaceDetails)
{
    resourceStore.Logger.Warn(...);
}
else
{
    alternateColorSpaceDetails = alternate;
}
```

Note the same unguarded recursion already exists for `Separation` / `DeviceN` alternates
(pre-existing, not introduced here), so a general depth counter threaded through
`GetColorSpaceDetails` would be the more thorough fix.

#### Resolution

**A first attempt guarded the whole arm on `cannotRecurse` and had to be replaced.** Putting
`if (cannotRecurse) return UnsupportedColorSpaceDetails.Instance;` at the top of
`case ColorSpace.ICCBased:` does stop the recursion, but `GetSecondaryColorSpace` passes
`cannotRecurse: true` unconditionally and is the shared helper for **five** things: the ICCBased
`/Alternate`, the Indexed base, the Pattern underlying space, and the Separation and DeviceN
alternates. So it also made ICCBased unusable as a *nested* space anywhere — and a Separation over
an ICCBased alternate is the bread and butter of the PDF/X files this commit targets. Measured:
**53 test failures** across ~10 real documents (`ColorIssue`, `TIKA-2054-0`,
`Pig Production Handbook`, `iron-ore-q2-q3-2013`, `GHOSTSCRIPT-702013-1`, `68-1990-01_A`), including
`ColorSpaceTests.SeparationIccColorSpacesWithForm`, `StencilIndexedIccColorSpaceImages` and
`DeviceNColorSpaceImages`.

**Shipped shape:** a dedicated `allowIccBased` flag on `GetSecondaryColorSpace`, so the prohibition
lands only where Table 66 states it.

```csharp
private static ColorSpaceDetails GetSecondaryColorSpace(..., bool applyDefaultSubstitution = true,
    bool allowIccBased = true)
{
    // ... in BOTH the name branch and the array branch, once the colour space is mapped:
    if (!allowIccBased && baseColorSpaceName == ColorSpace.ICCBased)
    {
        return UnsupportedColorSpaceDetails.Instance;
    }
```

with the ICCBased arm calling it as:

```csharp
// allowIccBased: false because 8.6.5.5 forbids an ICCBased /Alternate, and
// resolving one would re-enter this arm - on the very same /ColorSpace array when
// the alternate is a name - until the stack is exhausted.
var alternate = GetSecondaryColorSpace(alternateColorSpaceToken,
    imageDictionary, scanner, filterProvider, resourceStore,
    iccProfileCache, applyDefaultSubstitution: false, allowIccBased: false);
```

Indexed, Pattern, Separation and DeviceN keep the default and are unaffected. The alternate simply
resolves to `UnsupportedColorSpaceDetails`, which the existing code already handles by warning and
falling back to the device space implied by `/N`.

**Both branches have to be guarded, not just the array one.** A syntactic "is the `/Alternate` array
an ICCBased array?" check is insufficient: if `/Alternate` is a *name* mapping to ICCBased, the name
branch calls `GetColorSpaceDetails(ICCBased, imageDictionary, …)` against the **outer** image
dictionary, which re-reads the same `/ColorSpace` array and loops identically — on a single token,
with no indirect references involved.

**Tests added** to `IccBasedAlternateColorSpaceTests`:

- `AlternateNamingIccBased_IsRejectedRatherThanRecursed` — the one-token name case above.
- `AlternateCyclingBackToItsOwnIccBasedArray_IsRejected` — a genuine 2-object cycle
  (`/Alternate 7 0 R` → `[/ICCBased 8 0 R]` → stream 8's `/Alternate` → `7 0 R`).

Both were confirmed to reach the defect: with the parser fix reverted, that filtered run ends in
`Test Run Aborted` with a stack-overflow trace rather than a test failure. The tests carry a note
saying so, since a regression here kills the test host instead of failing an assertion.

### 2. `ICCBasedColorSpaceDetails.GetInitializeColor` clips against component 0's range only — **FIXED**

**File:** `src/UglyToad.PdfPig/Graphics/Colors/ColorSpaces/ICCBasedColorSpaceDetails.cs:406-416`

```csharp
double v = PdfFunction.ClipToRange(0.0, Range[0], Range[1]);
Span<double> buffer = stackalloc double[NumberOfColorComponents];
buffer.Fill(v);
return GetColor(buffer, intent);
```

8.6.5.5 (and the comment directly above this code) says all components initialise to 0.0 *"unless
the range of valid values **for a given component** does not include 0.0, in which case the nearest
valid value shall be substituted"* — i.e. per component. This clips 0 against component 0's pair and
then broadcasts the result to every component.

For `/Range [0.2 1 0 1 0 1]` the correct initial colour is `(0.2, 0, 0)`; this produces
`(0.2, 0.2, 0.2)`. `LabColorSpaceDetails.GetInitializeColor` gets this right, for comparison. Fix:

```csharp
Span<double> buffer = stackalloc double[NumberOfColorComponents];
for (int c = 0; c < buffer.Length; c++)
{
    buffer[c] = PdfFunction.ClipToRange(0.0, Range[2 * c], Range[2 * c + 1]);
}
```

#### Resolution

Shipped as the per-component loop above, with the comment spelling out why a single clip is wrong:

```csharp
// The substitution is per component, as "for a given component" says: /Range may exclude 0.0 from
// one component while leaving the others free, and clipping once against the first pair would
// carry that one component's substitute into all of them.
Span<double> buffer = stackalloc double[NumberOfColorComponents]; // 1, 3 or 4

for (int c = 0; c < buffer.Length; c++)
{
    buffer[c] = PdfFunction.ClipToRange(0.0, Range[2 * c], Range[2 * c + 1]);
}

return GetColor(buffer, intent);
```

On the open question of `Range` vs. `EffectiveRanges`: **`Range` was kept.** It is the colour space's
own declared valid range, which is what 8.6.8's "range of valid values" refers to; `GetColor` re-clips
against `EffectiveRanges` immediately afterwards, so the profile's authority over an L\*a\*b\*
encoding still applies; and keeping it means the only behaviour that changes is the bug. The two only
diverge when a profile declares non-`[0, 1]` ranges *and* the file also writes a non-default
`/Range`, in which case the explicit `/Range` deserves to win. The constructor already guarantees
`Range.Count == 2 * NumberOfColorComponents`, so the indexing is safe.

**Tests added** to `ICCBasedColorSpaceDetailsTests`:

- `GetInitializeColor_SubstitutesPerComponent_WithoutAProfile` — the alternate path.
- `GetInitializeColor_SubstitutesPerComponent_OnTheProfilePath` — via the existing
  `WithRange` / `RecordingTransform` helpers, covering all three cases in one assertion: range
  starting above 0.0 → its minimum, straddling 0.0 → 0.0, ending below 0.0 → its maximum.

Confirmed to catch the defect: against the unfixed code they fail with `[0.2, 0.2, -0.25]` instead of
`[0.2, 0, -0.25]`, and `g = 0.2` instead of `0`.

### 3. Output-intent management is silently skipped for uncoloured tiling patterns — **FIXED**

**Files:** `src/UglyToad.PdfPig/Graphics/PdfColorInfo.cs:130-146`,
`src/UglyToad.PdfPig/Graphics/ColorSpaceContext.cs:47-60`

`PdfColorInfo.ForPattern` takes no `IIccProfile` and never calls `TryManage`, and
`ColorSpaceContext.SetStrokingColor(operands, patternName)` does not pass one on the pattern branch
(only the `else` branch calls `GetOutputIntentIccProfile`).

The underlying colour of a `/PaintType 2` tiling pattern is an ordinary device colour selected in an
ordinary device colour space — exactly what `UseOutputIntent` is meant to manage. With
`UseOutputIntent = true`, `0 0 0 1 /P1 scn` renders a different black from `0 0 0 1 k` in the same
document, for no reason a user could predict.

Secondary effect: because the profile isn't retained, `PdfColorInfo.Resolved` won't re-manage the
underlying colour when a later `ri` changes the intent either.

Fix: give `ForPattern` the same optional `outputIntentProfile` parameter and run the underlying
colour through `TryManage`, exactly as `FromOperands` does.

**No test covers this** — `UncolouredTilingPatternColorTests.cs` exercises intent variation but never
output intents.

#### Resolution

The profile is now threaded down the pattern path exactly as it already was for an ordinary colour,
across the same three layers the bug spanned.

`PdfColorInfo.ForPattern` takes the profile and manages the underlying colour:

```csharp
public static PdfColorInfo ForPattern(PatternColorSpaceDetails patternColorSpace, NameToken patternName,
    double[]? operands, RenderingIntent intent, IIccProfile? outputIntentProfile = null)
{
    // ...
    if (underlyingColorSpace is null)
    {
        // No underlying colour at all - a coloured tiling pattern or a shading pattern - so there is
        // nothing for an output intent to manage and nothing to reconvert on a later intent either.
        return new PdfColorInfo(null, operands, null, patternColor, intent);
    }

    var underlyingColor = Convert(underlyingColorSpace, operands, intent);

    // Retained only when it applied, as on FromOperands: the field means "the profile this colour
    // was managed through", so a colour space the profile cannot express keeps null.
    if (outputIntentProfile is not null &&
        TryManage(underlyingColorSpace, underlyingColor, outputIntentProfile, intent, out var managed))
    {
        return new PdfColorInfo(underlyingColorSpace, operands, managed, patternColor, intent,
            outputIntentProfile);
    }

    return new PdfColorInfo(underlyingColorSpace, operands, underlyingColor, patternColor, intent);
}
```

`CurrentGraphicsState` gains the profile-taking overloads, mirroring how `SetStrokingColor` already
splits, and `ColorSpaceContext`'s two pattern branches pass `GetOutputIntentIccProfile(state)` — the
same call the non-pattern branch beside them was already making.

Three things fell out for free rather than needing code:

- **Re-management on a later `ri`.** `PdfColorInfo.Resolved` already re-runs `TryManage` generically
  and already carries `patternColor` through, so once `ForPattern` stores the profile the intent
  change works with no change to `Resolved`.
- **Eligibility.** `TryManage` routes through `GetEffectiveDeviceType`, so an underlying Separation
  over DeviceCMYK is managed (as its non-pattern equivalent is) while an underlying ICCBased is not
  (it converts through its own profile already, and managing it would apply two profiles).
- **The pattern itself is untouched.** It is selected by name and never converts; the colours its
  content stream paints with are managed when that stream is processed.

**Retention policy note.** The profile is stored only when management actually applied, matching
`FromOperands` and the documented meaning of the field ("the profile this colour *was managed
through*"). One consequence, pre-existing and shared with `FromOperands` rather than introduced here:
`TryConvert` can also fail because `TryGetTransform` declined *this particular intent*, and such a
colour then permanently loses management even if a later intent would have succeeded. Worth a
separate look if it ever bites; it is not specific to patterns.

**Tests added** to `OutputIntentDeviceColorTests` (which already had the profile scaffolding), driving
a real uncoloured tiling pattern out of a `/Pattern` page resource and a `[/Pattern /DeviceCMYK]`
colour space through `ColorSpaceContext`, so all three layers are covered end to end:

- `AnUncolouredTilingPatternsUnderlyingColourIsManagedThroughTheOutputIntent`
- `TheStrokingPatternOperatorManagesItsUnderlyingColourToo`
- `AManagedUnderlyingPatternColourFollowsALaterIntentChange`
- `AServiceThatDoesNotOptInLeavesTheUnderlyingPatternColourAlone` — the negative control

Confirmed to catch the defect: against the unfixed code the first three fail with the unmanaged
built-in conversion (`0` where `0.25` / `0.9` is expected), while the negative control passes both
before and after, as it should.

### 4. The image output-intent path pushes the device→profile component mapping onto every consumer — **FIXED**

**File:** `src/UglyToad.PdfPig/Graphics/Colors/Icc/OutputIntentColorManagement.cs:62-112`

The doc comment states the design goal clearly:

> "The counterpart of `TryConvert` for the image path, **so that the two cannot disagree about which
> images and colour spaces are eligible**"

Eligibility is indeed shared. The *mapping* is not. `GetDeviceImageTransform` declares a
1-component DeviceGray image eligible against a 3- or 4-component profile (lines 103-104), then hands
back an `IIccTransform` whose `NumberOfComponents` is 3 or 4 while the image's samples are 1 byte per
pixel. `IIccTransform.Transform` is contracted for `src.Length == pixelCount * NumberOfComponents`,
so the consumer must synthesise the expansion itself.

`PdfPig.Rendering.Skia` does exactly that, re-implementing `TryMapDeviceToProfileComponents` at byte
level (`SkiaImageExtensions.ApplyOutputIntent`, lines 365-400, including the `1 - g` inversion for
CMYK). That is the drift the comment says the design prevents — the scalar and byte mappings now live
in two repositories and can diverge silently.

Suggest one of:
- a core `MapDeviceSamplesToProfileComponents(ReadOnlySpan<byte>, ColorSpace, int)` companion to the
  existing `TryMapDeviceToProfileComponents`; or
- returning the required expansion alongside the transform (a small struct); or
- restricting `GetDeviceImageTransform` to exact component matches and documenting that gray images
  keep their built-in conversion.

#### Resolution

`GetDeviceImageTransform` now guarantees that **the transform it returns consumes the image's own
components**, not the profile's. Where they already match, the profile's transform is handed back
untouched; where they do not — the single DeviceGray channel against a 3- or 4-component profile,
which `canManage` establishes is the only mismatch possible — it is wrapped:

```csharp
// canManage above admits exactly one mismatch: a single DeviceGray channel against a 3- or
// 4-component profile. Hand back a transform that consumes the image's own components so no
// caller has to notice.
return transform.NumberOfComponents == colorSpace.BaseNumberOfColorComponents
    ? transform
    : new DeviceGrayExpandingTransform(transform);
```

The wrapper is a private `IIccTransform` reporting `NumberOfComponents => 1`. `ToRgb` delegates to the
existing `TryMapDeviceToProfileComponents`, so scalar and image paths cannot disagree about what a
grey means — which is what the original doc comment claimed and did not deliver.

**The packed path is a 256-entry lookup table, not a per-pixel expansion.** A one-component source has
only 256 possible values, so the wrapper converts those 256 once — building the ramp
(`g -> (0, 0, 0, 1 - g)` for CMYK, `g -> (g, g, g)` for RGB) and driving the profile a single time —
then reads each pixel's answer off the table:

```csharp
public void Transform(ReadOnlySpan<byte> src, Span<byte> dstRgb)
{
    byte[] table = lookup ??= BuildLookup();

    for (int p = 0; p < src.Length; p++)
    {
        int entry = src[p] * 3;
        int i = p * 3;

        dstRgb[i] = table[entry];
        dstRgb[i + 1] = table[entry + 1];
        dstRgb[i + 2] = table[entry + 2];
    }
}
```

The first attempt expanded every pixel into a 3-4x larger buffer and pushed all of it through the
profile — an `ArrayPool` rental sized to the image, plus one ICC conversion per pixel. For a
4000x4000 greyscale page against a CMYK intent that is a 64 MB transient and 16 million conversions,
against 768 bytes and 256 conversions here. The table is built lazily, so the scalar entry point
never pays for it.

**Tests added** to `OutputIntentColorManagementTests`:

- `MatchingComponentCounts_HandBackTheProfileTransformItself` — `Assert.Same`, the no-wrapping control
- `GrayImage_AgainstACmykProfile_YieldsATransformSizedForTheImage` — the `NumberOfComponents == 1` guarantee
- `GrayImage_AgainstACmykProfile_ExpandsEachSampleIntoTheBlackChannel`
- `GrayImage_AgainstAnRgbProfile_ReplicatesEachSample`
- `GrayImage_DrivesTheProfileOnceWhateverTheImageSize` — locks in the table, by call count
- `TheExpandingTransformMapsScalarConversionsToo` — both entry points agree

The two expansion tests originally asserted on the bytes the inner transform *received*, which the
lookup table legitimately changed; they now assert the RGB that comes out, which is the behaviour
that actually matters and is not coupled to how the wrapper drives the profile.

**`PdfPig.Rendering.Skia` updated:** `SkiaImageExtensions.ApplyOutputIntent` — 46 lines that
re-implemented the expansion — is deleted, and its caller is now two lines:

```csharp
byte[] managed = new byte[pixelCount * 3];
outputIntentTransform.Transform(imageSpan.Slice(0, pixelCount * numberOfComponents), managed);
```

---

## Design / API

### 5. Substantial undeclared breaking changes to the public API — **WON'T DO**

The commit message describes only additive functionality. These are source- and binary-breaking:

| Type | Change |
|---|---|
| `ColorSpaceDetails` | `GetColor`, `GetRgb`, `GetInitializeColor`, `Transform`, `Process` — the abstract members are replaced by new abstract overloads taking `RenderingIntent`. **Every external subclass fails to compile.** |
| `CurrentGraphicsState` | `CurrentStrokingColor` / `CurrentNonStrokingColor` setters are `[Obsolete(error: true)]` and throw `NotSupportedException` |
| `BaseStreamProcessor<T>` (public) | protected ctor gained a required `DictionaryToken? pageDictionary` |
| `XObjectFactory.ReadImage` (public) | gained a required `ParsingOptions options` |
| `ColorSpaceDetailsByteConverter.Convert` (6-arg, public) | gained a required `RenderingIntent intent` |
| `IResourceStore` (public interface) | gained 4 members — any external implementer breaks |

`PublicApiScannerTests` was updated for the six new *types*, but the scanner does not appear to catch
signature changes on existing ones.

Recommendations: call the breaks out explicitly in the release notes / bump the major version; and
for the two colour setters, prefer deleting them outright over an `[Obsolete(error: true)]` setter
that still exists in metadata and still throws at runtime for anyone binding late or via reflection.

#### Marked won't do

No release-note or versioning action will be taken, and the API shapes stand as committed. The table
above is kept as the record of what breaks, since it is the same list anyone diagnosing a downstream
build failure will need.

Two consequences worth being aware of rather than surprised by:

- The fix for **#6** adds one more entry to that list — `CurrentStrokingColor` and
  `CurrentNonStrokingColor` changing from `IColor` to `IColor?` is source-breaking for
  nullable-enabled consumers, on top of everything already listed.
- `ColorSpaceDetails`'s abstract members are the widest break here: any external subclass fails to
  compile, and the failure surfaces at the subclass rather than at the call site, so the error
  message will not point at PdfPig. Most breaks in this list are compile-time and loud; that one is
  compile-time but indirect.

### 6. `CurrentStrokingColor` is annotated non-nullable but returns `null` — **FIXED**

**File:** `src/UglyToad.PdfPig/Graphics/CurrentGraphicsState.cs`

The file dropped `#nullable disable` in this commit, so its annotations are now load-bearing. Both
colour getters are declared `IColor` and return `stroking.Color!`. `PdfColorInfo.Color` is
`IColor?`, and for a Pattern colour space's initial colour (`PatternColorSpaceDetails.GetInitializeColor`
is documented as *"Always returns null"*) it always is. `SetStrokingColor(IColor?)`'s own XML doc
says so:

> "or `null`, which is what a Pattern colour space's initial colour is, **and which
> `CurrentStrokingColor` then hands back**"

The `!` suppression converts a documented null into a silent NRE for any consumer that trusts the
annotation. These should be `IColor?`. (That is itself a source-breaking change for nullable-enabled
consumers, so it belongs with #5 rather than after it.)

Related, minor: both getters mutate the backing field (`stroking = stroking.Resolved(...)`).
Side-effecting property getters surprise debuggers and profilers; consider an explicit
`ResolveColors()`.

#### Resolution

Both properties are now `IColor?`, and the `!` suppression inside the getter is gone:

```csharp
public IColor? CurrentStrokingColor
{
    get
    {
        stroking = stroking.Resolved(RenderingIntent);
        return stroking.Color;
    }
    ...
}
```

Verified this introduces no new warnings anywhere in `UglyToad.PdfPig`: the two consumers were
already prepared for null — `PdfPath.StrokeColor` / `FillColor` are declared `IColor?`, and
`ContentStreamProcessor`'s `Letter` construction already carried an explicit `!`, which is now a
claim the caller is entitled to make rather than one papering over the property's own annotation.

The "related, minor" note above about side-effecting getters was **not** addressed and stands.

This is itself a source-breaking change for nullable-enabled consumers; see #5, which is won't-do, so
it goes out unannounced along with the rest.

### 7. `BaseType` now means two different things one level apart

`ICCBasedColorSpaceDetails` (new): `BaseType = ColorSpace.DeviceRGB` when a profile is in use —
i.e. *the space the output is actually in*.

`SeparationColorSpaceDetails:77`, `DeviceNColorSpaceDetails:87`, `IndexedColorSpaceDetails`:
`BaseType = AlternateColorSpace.Type` — i.e. *the declared type of the next space down* — while
`BaseNumberOfColorComponents` correctly forwards `AlternateColorSpace.BaseNumberOfColorComponents`.

So a Separation over a managed ICCBased reports `BaseType == ICCBased` with
`BaseNumberOfColorComponents == 3`. `GetDeviceImageTransform` switches on `BaseType` (line 94), and
here the mismatch happens to produce the right answer (no double management), but the invariant is
now inconsistent and a consumer branching on `BaseType` to size buffers will get it wrong. Either
make the nested spaces forward `.BaseType`, or document precisely what `BaseType` means.

### 8. Page-level `/OutputIntents` only reaches the page's own processor — **FIXED**

**File:** `src/UglyToad.PdfPig/Graphics/BaseStreamProcessor.cs:136-157`

`OutputIntents` is populated once, from the `pageDictionary` passed to the constructor. Any nested
processor a consumer constructs (form XObject, tiling pattern, shading, soft mask) has no page
dictionary, so `GetPageOutputIntents(null)` returns the **document catalog's** intents — silently
dropping a page-level override.

The Skia renderer papers over this from the outside:

```csharp
// SkiaStreamProcessor.Shading.cs:1087-1088
// not a page and never carries /OutputIntents, hence pageDictionary: null above.
processor.GetCurrentState().OutputIntents = GetCurrentState().OutputIntents;

// SkiaStreamProcessor.SoftMask.cs:80-81
var savedOutputIntents = maskState.OutputIntents;
maskState.OutputIntents = null;
```

That's a contract every consumer has to discover from the source. Consider an explicit
`IReadOnlyList<OutputIntent>? inheritedOutputIntents` constructor parameter (or an overload taking
the parent state), so a sub-processor inherits by construction rather than by post-hoc assignment.

#### Resolution

**The processor no longer takes a page dictionary at all.** `DictionaryToken? pageDictionary` existed
on `BaseStreamProcessor` for exactly one purpose — calling `resourceStore.GetPageOutputIntents` — so
it is replaced by the thing it was being used to compute:

```csharp
protected BaseStreamProcessor(
    …,
    in TransformationMatrix initialMatrix,
    IReadOnlyList<OutputIntent>? outputIntents,
    ParsingOptions parsingOptions)
```

`PageFactory` now resolves them at the call site with
`ResourceStore.GetPageOutputIntents(dictionary)`, and a nested processor passes the intents in force
where it was invoked. `ContentStreamProcessor` forwards the same parameter.

This removes the trap rather than documenting around it. There is no longer any argument a nested
processor can pass that quietly yields the document catalog's intents, because the processor never
consults the resource store for them — the question "which intents apply here?" is answered by
whoever knows, once, before construction. It also stops the processor knowing what a page dictionary
is, which was never its concern.

**A first attempt used two constructors** — one taking `pageDictionary`, one taking `outputIntents` —
with the parameter order juggled so that a `null` argument stayed unambiguous between them. That
juggling existed solely to keep existing call sites compiling, which is not a constraint worth
honouring for an unreleased API with one consumer. Replacing the parameter outright is simpler, has
no ambiguity to design around, and deletes a constructor rather than adding one.

Nothing in PdfPig itself constructs a nested processor — form XObjects are handled inline on the
state stack — so the benefit is entirely consumer-facing.

**Tests added** in a new `OutputIntentInheritanceTests`, with a minimal concrete
`BaseStreamProcessor<object>`:

- `APageProcessorTakesThePagesOwnIntents`
- `APageProcessorFallsBackToTheCatalog`
- `ANestedProcessorTakesTheIntentsItIsGiven` — a page override survives into nested content
- `ANestedProcessorTakesNullAsNoneInEffect` — soft-mask suppression stays suppressed
- `ANestedProcessorCannotReachForTheCatalog` — the trap this finding is about

**`PdfPig.Rendering.Skia` updated:** `SkiaStreamProcessor` takes `IReadOnlyList<OutputIntent>?`,
`SkiaPageFactory` resolves them, and the tiling-pattern sub-processor in
`SkiaStreamProcessor.Shading.cs` now inherits by construction — the post-construction
`processor.GetCurrentState().OutputIntents = …` patch is gone. Its soft-mask save/restore is a scoped
mutation of an existing state and is unaffected.

**Related tidy-up in `CurrentGraphicsState`.** The same back-compat instinct had produced four
convenience overloads (`SetStrokingColor`, `SetNonStrokingColor`, `SetStrokingPatternColor`,
`SetNonStrokingPatternColor`, each in a 2-or-3-arg form delegating to a profile-taking one). None had
a production caller. They are now single methods with an optional
`IIccProfile? outputIntentProfile = null`: same call sites, four fewer public members, and no
ambiguous `cref` in the XML docs.

### 9. `IccProfileReference` exposes `ReadOnlySpan<byte>` properties — **FIXED**

**File:** `src/UglyToad.PdfPig/Graphics/Colors/Icc/IccProfileReference.cs:28-40`

`ICCVersion` and `CheckSum` are `ReadOnlySpan<byte>` properties on a public, non-ref data class. They
cannot be captured in a lambda, used in an async method, stored in a field, or consumed from F#/VB.
For a small metadata holder that is read once, `ReadOnlyMemory<byte>` or `IReadOnlyList<byte>` costs
nothing and is far friendlier.

#### Resolution

Both are now `ReadOnlyMemory<byte>`, still backed by the same `byte[]` fields, so the class stays
allocation-free on read and the "empty when not provided" contract is unchanged. Nothing in the
library consumed either property, so the change is confined to the public surface.

---

## Performance

### 10. Output intent re-selected on every colour-setting operator — **FIXED**

**File:** `src/UglyToad.PdfPig/Graphics/ColorSpaceContext.cs:180-186`

```csharp
private IIccProfile? GetOutputIntentIccProfile(CurrentGraphicsState state)
{
    var iccService = resourceStore.IccProfileService;
    return iccService?.UseOutputIntent == true ?
        OutputIntentColorManagement.GetDeviceProfile(state.OutputIntents, iccService)
        : null;
}
```

This runs on every `g` / `rg` / `k` / `sc` / `scn` / `cs` / `CS`, and each call walks the
`/OutputIntents` array doing ordinal string comparisons on `/S`. The result depends only on
`state.OutputIntents` and the service — both effectively constant for a page. Caching the resolved
`IIccProfile?` next to `OutputIntents` on `CurrentGraphicsState` (invalidated when `OutputIntents` is
assigned) removes it from the hot path entirely. `ColorOperatorBenchmarks` was added in this commit,
so the number is presumably known — worth confirming against a content stream with thousands of
colour operators.

#### Resolution

Not a cache — the lookup is gone. **`CurrentGraphicsState` now carries the answer instead of the
material to work it out from:** `IReadOnlyList<OutputIntent>? OutputIntents` is replaced by
`IIccProfile? OutputIntentProfile`, and `ColorSpaceContext.GetOutputIntentIccProfile` is deleted
outright — its seven call sites now read a field.

Selecting an intent is a per-page decision (the intents, the service's opt-in and its preferred
subtype are all fixed for the page), so it is made once, by the party that has both halves of the
question:

```csharp
// IResourceStore
IIccProfile? GetPageOutputIntentProfile(DictionaryToken? pageDictionary);
```

`ResourceStore` owns both the parsed intents and the configured `IIccProfileService`, so this is a
better home for the decision than the colour-space context, which had to reach for the service on
every `g` / `rg` / `k` / `sc` / `scn` / `cs` / `CS`.

Consequences beyond the hot path:

- `GetDeviceImageTransform(colorSpace, intent, outputIntents, service)` loses two parameters and
  becomes `GetDeviceImageTransform(colorSpace, intent, profile)` — it was only ever using them to
  reach the same profile.
- `BaseStreamProcessor`'s constructor takes `IIccProfile? outputIntentProfile` rather than the intent
  list, which composes with #8: a page processor passes
  `resourceStore.GetPageOutputIntentProfile(pageDictionary)`, a nested one passes the profile in
  force where it was invoked.
- Suppressing management for a soft-mask group is still one assignment, now of a profile rather than
  a list.

A consumer that wants the intents themselves still has `IResourceStore.DocumentOutputIntents` and
`GetPageOutputIntents`; the graphics state's copy only ever existed to answer this one question.

**Tests:** `OutputIntentInheritanceTests` was rewritten around the profile, with distinguishable
profiles so a test can say *which* one reached the state — page override, catalog fallback, a service
that does not opt in, nested inheritance, suppression, and survival of a `DeepClone`.

No benchmark was run, so no speedup is claimed here: the honest statement is that per-operator work
went from a list scan with ordinal string comparisons to a field read.

### 11. JPX-embedded ICC profiles are re-parsed on every image read

**File:** `src/UglyToad.PdfPig/Images/Jpeg2000Helper.cs:114-127`

The comment is honest about why there's no cache ("a slice of the image's own codestream rather than
a stream object the document can point at twice"), but `GetJpxColorSpaceDetails` is called from
`XObjectFactory.ReadImage`, and `PageContent.GetImages()` re-reads every image on each enumeration.
So enumerating a page's images twice parses each JPX profile twice, and re-rendering a page parses
them again. The containing XObject's `IndirectReference` *is* a stable key — `IccProfileCache` could
take an explicit-key overload.

### 12. `IccProfileCache` doesn't cache direct (non-indirect) profile streams

**File:** `src/UglyToad.PdfPig/Util/IccProfileCache.cs:36-39`

```csharp
if (profileToken is not IndirectReferenceToken reference)
{
    return DecodeAndParse(...);   // never cached
}
```

Correct and safe, and `ResourceStore.loadedColorSpaceDetailsCache` covers the common path, but a
direct profile stream inside a form XObject resource dictionary re-decodes and re-parses on every
resolution. A content-hash fallback key (as `IIccProfileService`'s own doc recommends for
implementations) would close it. At minimum, a comment saying why it's acceptable.

### 13. `ICCBasedColorSpaceDetails.Range` default is built with three LINQ hops

**File:** `src/UglyToad.PdfPig/Graphics/Colors/ColorSpaces/ICCBasedColorSpaceDetails.cs:147-150`

```csharp
Range = range ?? Enumerable.Range(0, NumberOfColorComponents)
    .Select(x => new[] { 0.0, 1.0 })
    .SelectMany(x => x)
    .ToArray();
```

Three enumerators plus N two-element arrays to produce at most 8 doubles, in a constructor that runs
per colour space. There are only three possible results — three `static readonly double[]` (or a
four-line loop) is cheaper and reads better.

---

## Robustness

### 14. Values from a third-party `IIccTransform` are never clamped — **FIXED**

`IIccTransform.ToRgb` is documented as returning `[0..1]`, but the whole design elsewhere assumes
third-party implementations misbehave (`IsUsable` probes both entry points; `TryToRgb` catches;
`IccProfileParser` catches). The output is then fed to:

- `new RGBColor(r, g, b)` — no clamping in the constructor;
- `ColorSpaceDetails.ConvertToByte` — `(byte)Math.Round(v * 255, AwayFromZero)` with **no clamp**.

An unchecked `double → byte` conversion outside `[0, 255]` is unspecified in C# and in practice
wraps; `NaN` produces 0. A service returning slightly out-of-gamut values (common for
absolute-colorimetric transforms) yields wrapped garbage pixels rather than clipped ones. Clamping in
`ConvertToByte` (and in `TryToRgb`) is consistent with the rest of the defensive posture here and
costs one `Math.Clamp`.

#### Resolution

Fixed at both levels, and the byte conversion got faster on the way.

**At the ICC boundary.** A new internal `IccTransformExtensions.TryToRgbClipped` is the single place
a colour is read back out of an `IIccTransform`. It clips each component into `[0, 1]` and reports
failure for a `NaN`:

```csharp
public static bool TryToRgbClipped(this IIccTransform transform, ReadOnlySpan<double> values,
    out double r, out double g, out double b)
```

The NaN case is deliberately not clipped to zero. There is no nearest valid value to substitute, and
both callers — `ICCBasedColorSpaceDetails.TryToRgb` and `OutputIntentColorManagement.TryConvert` —
already have a well-defined answer for "the profile could not convert this colour": the alternate
colour space, or the built-in device conversion. Reporting failure hands the colour to that instead
of painting it black.

**In `ConvertToByte`**, which is the last line of defence for every colour space rather than only the
ICC ones:

```csharp
// Written as a pair of positive tests so that NaN, which compares false against everything,
// lands on 0 rather than falling through.
if (!(componentValue > 0.0))
{
    return 0;
}

if (componentValue >= 1.0)
{
    return 255;
}

// Now that the value is known to be in (0, 1), adding a half and truncating rounds away from
// zero exactly as Math.Round(x, MidpointRounding.AwayFromZero) did, without the call.
return (byte)(componentValue * 255.0 + 0.5);
```

Once the range is established the `Math.Round` call is redundant, so the clamped version is *cheaper*
than the unclamped one it replaces — and this runs per component per pixel on every non-passthrough
image path.

**Tests** in a new `IccTransformClippingTests`: out-of-range components clipped on both the `GetColor`
and `GetRgb` paths, a NaN falling back to the alternate, and `ConvertToByte` across the boundaries.
Six fail against the unclamped code — `2.0`, `∞` and `-1.0` all produce wrong bytes — while
`-0.0001`, `-∞` and `NaN` happen to land on 0 anyway, which is precisely the point about undefined
behaviour: some inputs look fine and others do not, and which is which is not something to rely on.
The rounding-parity cases pass both before and after, confirming the arithmetic change is not a
behaviour change.

### 15. Two more pieces of unsynchronised mutable state on the document-scoped `ResourceStore` — **WON'T DO**

`IccProfileCache.byReference` (plain `Dictionary<IndirectReference, IIccProfile?>`) and
`ResourceStore.pageOutputIntents` (plain `Dictionary`). `ResourceStore` already holds several
unsynchronised dictionaries, so this is consistent rather than a regression — but it is document
scoped and reached from page parsing, which downstream renderers do run in parallel (Caly renders
pages concurrently). `DocumentOutputIntents` uses a thread-safe `Lazy<T>`, which makes the
inconsistency more visible. Either use `ConcurrentDictionary` (cheap — these are cold paths) or put
an explicit "not thread-safe" note on `IResourceStore`.

#### Marked won't do

`ResourceStore` stays as it is. The two new dictionaries are consistent with the several that were
already there, so this changes nothing about the type's existing (undocumented) threading contract —
it is a pre-existing property of `ResourceStore` that this commit neither improves nor worsens.

Worth keeping in mind: it does mean a consumer parsing pages of one document concurrently is relying
on undocumented behaviour, and the ICC cache widens the window slightly because profile parsing is
slower than the lookups the other caches do.

---

## Hygiene

### 16. Whitespace / line-ending normalisation is mixed into the functional diff — **WON'T DO**

`ColorSpaceDetailsParser.cs` reports **239 added / 212 removed** lines; with `--ignore-cr-at-eol` the
real change is **42 / 15**. The file previously had mixed CRLF/LF terminators and is now uniform
CRLF — a genuine improvement, but it buries the actual change. Several other files carry
`using`-reordering and trailing-whitespace-only edits (`BaseStreamProcessor.cs`,
`InlineImageBuilder.cs`, `PdfDocumentFactory.cs`, `OutputIntentsFactory.cs`,
`DeviceCmykColorSpaceDetails.cs`, `XObjectFactory.cs`).

For a change of this size, splitting "normalise line endings and usings" into its own commit would
make the substantive review far cheaper. Note also that editing these files with LF-emitting tools
re-introduces the churn — worth a `.gitattributes` `* text=auto eol=crlf` if there isn't one.

#### Marked won't do

`4999954d` stands as committed; the normalisation will not be split out retroactively.

The forward-looking half of the note still applies and is cheap to act on separately if wanted: the
repository's `.gitattributes` currently declares only binary types (`*.pfa`, `*.pfb`, `*.bin`,
`*.pdf`, `*.ttf`) and says nothing about text, so whether a `.cs` file ends up CRLF or LF depends on
whichever tool last wrote it. Reviewing with `git diff --ignore-cr-at-eol` sidesteps it in the
meantime — that is how the real size of this commit was measured for the header above.

### 17. Documentation defects — **MOSTLY FIXED**

- `OutputIntent.cs:13` — the class summary ends with a stray sentence fragment, `remaining entries.`
  (trailing whitespace too). Looks like a truncated paste.
- `"PDF 2. 0"` (space inside the version number) in `OutputIntent.cs:63,69,77` and
  `IccProfileReference.cs`.
- `IResourceStore.cs` and `ParsingOptions.cs` both end without a trailing newline.
- `OutputIntentParser.cs:20` — leftover `// TODO - Can we use IEnumerable<> instead of
  IReadOnlyList<> to make the call lazy?`. Either answer it (no — the list is enumerated repeatedly
  by `SelectForColorManagement`) or drop it.

#### Resolution

The stray `remaining entries.` fragment, all three `PDF 2. 0` typos and the `TODO` are fixed in the
working tree. The only part left is the missing trailing newline on `IResourceStore.cs` and
`ParsingOptions.cs`.

### 18. Redundant checks

- `OutputIntentColorManagement.TryConvert:51-52` —
  `!profile.TryGetTransform(intent, out var transform) || transform is null`. `TryGetTransform` is
  annotated `[NotNullWhen(true)]`; the second clause is dead for any well-behaved implementation and
  misleading for one that isn't (it silently accepts a `true`/`null` return instead of treating it as
  the contract violation it is).
- `ColorSpaceContext.GetOutputIntentIccProfile` checks `UseOutputIntent`, and `GetDeviceProfile`
  checks it again on its first line.

### 19. Inconsistent argument validation on `ICCBasedColorSpaceDetails`

Three entry points, three behaviours for a wrong component count: `GetColor` throws
`ArgumentException`; `Process` silently zero-fills/truncates via `Normalise`; `GetRgb` reaches
`Clip(values, …)`, which indexes `values[c]` up to `NumberOfColorComponents` and throws
`IndexOutOfRangeException`. This matches the rest of the codebase (no `GetRgb` validates anywhere),
so it is a note rather than a defect — but the new `Normalise` helper makes a uniform policy easy if
one is wanted.

---

## Test coverage

Strong: ~3,800 lines of new tests, including `ICCBasedColorSpaceDetailsTests` (1,212 lines),
`OutputIntentParserTests` (606), `ResourceStorePageOutputIntentTests` (369),
`IccProfileCacheTests` (194), `OutputIntentSelectionTests` (210),
`InitializeColorRenderingIntentTests` (257), `RenderingIntentAffectsOutputTests` (256),
`UncolouredTilingPatternColorTests` (304), `DefaultDecodeTests` (174). Full suite green.

Closed since the review (18 tests added alongside the fixes):

| Gap | Finding |
|---|---|
| Self-referential / cyclic `/Alternate` on an ICCBased space | #1 — **covered** |
| `GetInitializeColor` with an asymmetric `/Range` (e.g. `[0.2 1 0 1 0 1]`) | #2 — **covered** |
| Output-intent management of an uncoloured tiling pattern's underlying colour | #3 — **covered** |
| A DeviceGray image managed through a 4-component (CMYK) output intent | #4 — **covered** |
| Page-level `/OutputIntents` reaching a nested form-XObject / pattern processor | #8 — **covered** |

Still open:

| Gap | Finding |
|---|---|
| An `IIccTransform` returning out-of-range or `NaN` components | #14 |

---

## Suggested order of work

- ~~**#1** — unbounded recursion on hostile input; process-killing and uncatchable.~~ **Done.**
- ~~**#2** — per-component initial colour.~~ **Done.**
- ~~**#3** — uncoloured tiling pattern skipped output-intent management.~~ **Done.**
- ~~**#4** — image path leaked the DeviceGray expansion to consumers.~~ **Done.**
- ~~**#6** — `CurrentStrokingColor` nullability.~~ **Done.**
- ~~**#8** — nested processors lost page-level output intents.~~ **Done.**
- ~~**#9** — `IccProfileReference` span properties.~~ **Done.**
- ~~**#10** — output intent re-selected on every colour operator.~~ **Done.**
- ~~**#14** — unclamped third-party transform output.~~ **Done.**
- ~~**#5**, **#15**, **#16**.~~ **Won't do.**

Every bug, design gap, performance item and robustness item is now closed. What remains:

1. **#7** — decide what `BaseType` means and document it. The last item that is about correctness of
   understanding rather than tidiness, and #4 leans on the answer.
2. **#18**, **#19**, and the last of **#17** — tidy-ups; `#18`'s redundant `transform is null` check
   is already gone, absorbed by the #14 rewrite.
3. **#11**, **#12**, **#13** — caching and allocation, all minor.

`PdfPig.Rendering.Skia` has been updated alongside throughout: it no longer re-implements the
DeviceGray expansion (#4), patches `OutputIntents` after construction (#8), or passes the intent list
and service to the image path (#10). Its 538 tests pass on all four of its target frameworks.
