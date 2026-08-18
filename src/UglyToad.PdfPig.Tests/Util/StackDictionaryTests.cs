namespace UglyToad.PdfPig.Tests.Util
{
    using System.Collections.Generic;
    using PdfPig.Util;
    using Xunit;

    public class StackDictionaryTests
    {
        [Fact]
        public void HigherLevelsShadowLowerOnes()
        {
            var stack = new StackDictionary<string, int>();

            stack.Push();
            stack["a"] = 1;
            stack["b"] = 2;

            stack.Push();
            stack["a"] = 10;

            Assert.Equal(10, stack["a"]);
            Assert.Equal(2, stack["b"]);
        }

        [Fact]
        public void PopRestoresTheShadowedValue()
        {
            var stack = new StackDictionary<string, int>();

            stack.Push();
            stack["a"] = 1;
            stack.Push();
            stack["a"] = 10;

            stack.Pop();

            Assert.Equal(1, stack["a"]);
        }

        [Fact]
        public void PushedLevelIsNotMutatedByALaterWrite()
        {
            var cached = new Dictionary<string, int> { { "a", 1 } };

            var stack = new StackDictionary<string, int>();
            stack.Push(cached);

            stack["a"] = 99;
            stack["b"] = 100;

            Assert.Equal(1, cached["a"]);
            Assert.False(cached.ContainsKey("b"));
        }

        [Fact]
        public void WriteToAPushedLevelIsVisibleThroughTheStack()
        {
            var cached = new Dictionary<string, int> { { "a", 1 } };

            var stack = new StackDictionary<string, int>();
            stack.Push(cached);

            stack["a"] = 99;

            Assert.Equal(99, stack["a"]);
        }

        [Fact]
        public void TheSameLevelCanBePushedTwiceIndependently()
        {
            var cached = new Dictionary<string, int> { { "a", 1 } };

            var stack = new StackDictionary<string, int>();
            stack.Push(cached);
            stack.Push(cached);

            stack["a"] = 99;
            Assert.Equal(99, stack["a"]);

            stack.Pop();

            Assert.Equal(1, stack["a"]);
            Assert.Equal(1, cached["a"]);
        }

        [Fact]
        public void FlattenPrefersHigherLevels()
        {
            var stack = new StackDictionary<string, int>();

            stack.Push();
            stack["a"] = 1;
            stack["b"] = 2;
            stack.Push(new Dictionary<string, int> { { "a", 10 } });

            var flattened = stack.Flatten();

            Assert.Equal(10, flattened["a"]);
            Assert.Equal(2, flattened["b"]);
        }
    }
}
