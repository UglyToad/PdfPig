# Code review: `NearestNeighbourWordExtractor`

This review covers `UglyToad.PdfPig.DocumentLayoutAnalysis/WordExtractor/NearestNeighbourWordExtractor.cs` and the code it calls on the hot path: `Clustering.NearestNeighbours`, `Clustering.GroupIndexes`, `KdTree<T>`, and `Word` construction.

I confirmed each bug with a temporary xUnit test on net9.0; the repro code is in the appendix.

---

## Bugs

### 1. The k-d tree search can return the pivot as its own nearest neighbour, which splits words (confirmed)

`KdTree.cs:222-223`: a leaf skips the pivot, but an internal node doesn't. It takes itself as the current best with no `Equals(pivot)` / index check:

```csharp
var currentNearestNode = node;
var currentDistance = distance(node.Value, pivotPoint);   // may be the pivot itself
```

When the pivot sits on an internal node, its own distance is its End→Start distance, which is its advance width. That causes several problems:

- If the real neighbour is further away than the letter's own width, the search returns the letter itself.
- The search also prunes other branches using that too-small distance.
- The self-match then passes `dist < maxDist` whenever `width < 0.2 * max(width, pointSize)`. That applies to narrow glyphs, and always to zero-width ones. The result is `indexes[e] = e` (a self-loop), and the real forward link is lost.
- At the parent level, `!newNode.Element.Equals(pivot)` discards the whole subtree's result when that subtree's root was the pivot. A valid second-best candidate in that subtree is lost too.

**Repro:** "abc" + "i" (width 1, followed by a 1.5pt gap; maxDist = 2) + "def".

- Expected: 1 word.
- Actual: `abci | def`.

**Fix:** pass the pivot's index into the search. Skip `node.Index == pivotIndex` at both leaf and internal nodes, and start from `currentDistance = +inf` when skipping. This also replaces the virtual `Equals` calls with an int compare. Those calls box when `T` is a struct.

### 2. Letters in a word follow graph traversal order, not reading order (confirmed)

`Clustering.cs:85` + `DfsIterative`: a group's letters come out in DFS stack order, not chain order. `Word` then treats that order as the text order ("letters ... in the correct order").

**Repro:** "abc" drawn in the order b, a, c.

- Expected: `"abc"`.
- Actual: `"bac"`.

The same happens whenever a group branches, e.g. two letters pointing to the same neighbour (fake bold, overprinting): DFS pops later siblings first.

**Fix options:**

- Cheapest: sort each group by index, which gives content-stream order. That fixes branching but not out-of-order drawing.
- Better: order by position along the text direction. The orientation bucket is already known, so sort by X for Horizontal, -X for Rotate180, and so on, falling back to index for `Other`. This changes behaviour, so it needs a decision.

### 3. With `GroupByOrientation` (the default), word order differs between runs (confirmed)

`NearestNeighbourWordExtractor.cs:79-94`: buckets run in `Parallel.ForEach` and are appended to `results` under a lock in whatever order the threads finish.

**Repro:** 1,000 horizontal and 1,000 Rotate270 letters, extracted 200 times. 144 of 200 runs gave a different word order from the first run.

Anything downstream that depends on word order (reading order, tests, diffs) becomes flaky.

**Fix:** process the buckets sequentially; the inner `Parallel.For` already parallelises the work. If you keep the outer loop parallel, write each bucket's words to `results[i]` and concatenate afterwards.

### Minor issues

- **`MaxDegreeOfParallelism = 0` or `< -1` throws.** The outer loop maps it to `ProcessorCount`, but the raw value goes to the inner `new ParallelOptions { MaxDegreeOfParallelism = 0 }`, which throws `ArgumentOutOfRangeException`. Validate the value once, in the options or the constructor.
- **Misleading comment.** Line 69 says "thread-safe collection to avoid lock contention", but the code is a `List` + `lock`.
- **Duplicate check.** The null/empty check in the private `GetWords` (line 126) duplicates the public one.

---

## Performance (speed + memory)

These are roughly in order of payoff for a typical page (2-10k letters, ~95% horizontal).

### P1. Avoid bucketing and nested parallel loops in the common single-orientation case

Today every letter is copied into one of 5 `List<Letter>`s, which grow by doubling. The code then goes through Partitioner, `Parallel.ForEach`, a closure and a lock to reach the real work.

Instead:

- Count orientations in one pass.
- If they are all the same, call the core directly on `letters`.
- Otherwise, size the bucket arrays exactly from the counts and loop over them sequentially (this also fixes bug 3).

### P2. Drop `new List<Word>(letters.Count)` + `TrimExcess()`

See lines 70 and 95. The list is sized to the letter count, which is about 5x too big. `TrimExcess` then copies it again. A page with N letters produces about N/5 words.

### P3. Replace `GroupIndexes` with union-find (or CSR adjacency)

Today's cost:

- One `List<int>` per letter, each growing an `int[4]` on first add. That is about 2N objects, roughly 70 B per letter of pure garbage.
- A `Stack<int>` and a `List<int>` per group.
- A `List<List<int>>` holding the groups.

Every node has out-degree <= 1, so union-find over a single `int[] parent` is O(N·α(N)) with one allocation:

1. `union(i, indexes[i])`.
2. Count group sizes per root.
3. Fill an exact-size `Letter[]` per group, visiting `i` in ascending order.

Because step 3 visits indexes in ascending order, each group comes out sorted by index. That fixes the branching half of bug 2 for free.

### P4. Remove the triple materialisation of groups

Today the path is:

1. `GroupIndexes` builds a `List<List<int>>`.
2. Each group becomes `group.Select(i => elements[i]).ToList()`: an iterator, a closure and a `List<T>`, plus the `yield` state machine.
3. The extractor calls `.ToList()` again (line 136).
4. Everything is copied into `words`, which has no initial capacity.

Build `Word`s directly from the exact-size `Letter[]` groups into `new List<Word>(groupCount)`. Keep the public `IEnumerable` API of `Clustering.NearestNeighbours` as a thin wrapper over an internal method that returns arrays.

  > The same pattern is still left in DocstrumBoundingBoxes, which is outside P4's scope (P4 covered only word extraction). GetLines (line 337) and GetBlocks (line 411) call the lazy public NearestNeighbours overloads and then .ToList() the result. That's one iterator plus one list
  copy of the groups per call, not of the elements, so it's small. Eager internal versions of those two overloads, like NearestNeighbourGroups, would remove it.

### P5. Make `Parallel.For` adaptive

Each nearest-neighbour query takes a few hundred nanoseconds. For small pages, or when callers already parallelise across pages, the `Parallel.For` overhead and oversubscription cost more than the work itself.

- Go sequential below a threshold (~1-2k letters).
- Otherwise use `Partitioner.Create(0, n, chunk)`, so that each task runs a tight loop instead of one delegate call per letter.

### P6. Speed up the k-d tree build and shrink its memory

- **Build cost.** Every level does a full sort, giving O(N log² N). Quickselect / nth_element around the median gives O(N log N).
- **Node size.** Every node is a class of ~64 B, plus a virtual `IsLeaf` call. An array-backed implicit tree (struct nodes, int indexes) would halve the memory and improve cache locality during queries.
- **Delegate calls.** Compute the pivot and candidate points once into a `PdfPoint[]`, instead of calling `l => l.EndBaseLine` through a delegate on every query.

### P7. Streamline the nearest-neighbour search

- The recursion returns `(KdTreeNode<T>, double?)` at every level, i.e. nullable doubles and tuple copies. Pass `ref bestIndex, ref bestDist` down instead, seed `bestDist = +inf`, and drop the null checks.
- Compare indexes instead of calling `Equals` (see bug 1).
- Optionally special-case the default `Distances.Euclidean` / `Manhattan`:
  - Compare squared distances for Euclidean.
  - Avoid a delegate call per visited node.
  - Fall back to the delegate for custom measures.

### P8. `Word` construction (core project, same hot path)

- **Text building.** Each word allocates a `StringBuilder` (16-char default, regrows).
  - Single-letter words: use `letter.Value` directly.
  - Otherwise, sum the lengths first and use `string.Create` / `string.Concat`.
- **`GetBoundingBoxOther`.** It uses `SelectMany` + `Distinct` + four separate `Min`/`Max` passes over a deferred query. That re-transforms every point 4 times and allocates arrays per letter. A single loop fixes this. It only affects skewed text, but there the cost is large.

---

## Suggested order

1. **Fix bug 1** (self-neighbour search). It is a small change and a correctness fix. Some expected word counts in `NearestNeighbourWordExtractorTests` may change and should be checked against the PDFs.
2. **Process buckets sequentially**, with a fast path for a single orientation. This fixes bug 3 and covers P1 and P2.
3. **Switch `GroupIndexes` to union-find**, with index-ordered, exact-size groups, and remove the `ToList` chain. This covers P3 and P4 and fixes the branching part of bug 2.
4. **Decide on the order of letters within a word** when letters are drawn out of order (the rest of bug 2). This changes behaviour.
5. **Optimise the k-d tree internals** (P6, P7), measured with BenchmarkDotNet.

---

## Appendix: repro tests

These were run on net9.0 and all 3 failed as described above.

```csharp
namespace UglyToad.PdfPig.Tests.Dla
{
    using UglyToad.PdfPig.Content;
    using UglyToad.PdfPig.Core;
    using UglyToad.PdfPig.DocumentLayoutAnalysis.WordExtractor;
    using UglyToad.PdfPig.Graphics.Core;
    using UglyToad.PdfPig.PdfFonts;

    public class NearestNeighbourWordExtractorReviewTests
    {
        private static Letter H(string v, double x, double w, double y = 0)
        {
            var bbox = new PdfRectangle(x, y, x + w, y + 7);
            return new Letter(v, bbox, bbox, new PdfPoint(x, y), new PdfPoint(x + w, y), w, 1,
                (FontDetails)null, TextRenderingMode.Fill, null, null, 10, 0);
        }

        private static Letter V(string v, double x, double y, double h)
        {
            // Rotate270: baseline goes up
            var bbox = new PdfRectangle(new PdfPoint(x - 7, y), new PdfPoint(x - 7, y + h), new PdfPoint(x, y), new PdfPoint(x, y + h));
            return new Letter(v, bbox, bbox, new PdfPoint(x, y), new PdfPoint(x, y + h), h, 1,
                (FontDetails)null, TextRenderingMode.Fill, null, null, 10, 0);
        }

        [Fact] // Bug 1 - actual: "abci | def"
        public void SelfNeighbourSplitsWord()
        {
            var letters = new List<Letter>();
            double x = 0;
            foreach (var c in "abc") { letters.Add(H(c.ToString(), x, 5)); x += 5; }
            letters.Add(H("i", x, 1)); x += 1 + 1.5;
            foreach (var c in "def") { letters.Add(H(c.ToString(), x, 5)); x += 5; }

            var words = NearestNeighbourWordExtractor.Instance.GetWords(letters).ToList();
            Assert.Single(words);
        }

        [Fact] // Bug 2 - actual: "bac"
        public void OutOfOrderLetters()
        {
            var a = H("a", 0, 5);
            var b = H("b", 5, 5);
            var c = H("c", 10, 5);
            var words = NearestNeighbourWordExtractor.Instance.GetWords(new[] { b, a, c }).ToList();
            Assert.Equal("abc", words.Single().Text);
        }

        [Fact] // Bug 3 - actual: 144 / 200 runs differ
        public void WordOrderIsDeterministic()
        {
            var letters = new List<Letter>();
            for (int line = 0; line < 50; line++)
            {
                for (int i = 0; i < 20; i++) letters.Add(H("h", i * 5, 5, line * 20));
                for (int i = 0; i < 20; i++) letters.Add(V("v", 500 + line * 20, i * 5, 5));
            }

            var reference = string.Join(",", NearestNeighbourWordExtractor.Instance.GetWords(letters).Select(w => w.Text));
            int diffs = 0;
            for (int k = 0; k < 200; k++)
            {
                var s = string.Join(",", NearestNeighbourWordExtractor.Instance.GetWords(letters).Select(w => w.Text));
                if (s != reference) diffs++;
            }
            Assert.Equal(0, diffs);
        }
    }
}
```
