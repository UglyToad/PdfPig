namespace UglyToad.Pdf.Text
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Logging;
    using Util.JetBrains.Annotations;

    public class TextSectionParser
    {
        private readonly ILog log;

        public TextSectionParser(ILog log)
        {
            this.log = log;
        }

        public IReadOnlyList<object> ReadTextObjects(ITextScanner textScanner)
        {
            bool textSectionActive = false;
            var result = new List<List<ITextObjectComponent>>();
            var sections = new List<ITextObjectComponent>();
            while (textScanner.Read())
            {
                sections.Add(textScanner.CurrentComponent);

                switch (textScanner.CurrentComponent.Type)
                {
                    case TextObjectComponentType.BeginText:
                        if (textSectionActive)
                        {
                            throw new InvalidOperationException("Found a begin text (BT) nested in another.");
                        }

                        textSectionActive = true;
                        break;
                    case TextObjectComponentType.EndText:
                        textSectionActive = false;

                        result.Add(sections);
                        sections = new List<ITextObjectComponent>();
                        break;
                }
            }

            foreach (var section in result)
            {
                if (section[0].Type == TextObjectComponentType.BeginText)
                {
                    ProcessTextSection(section, true);
                }

            }

            return result;
        }

        private object ProcessTextSection(IReadOnlyList<ITextObjectComponent> components, bool isLenientParsing)
        {
            if (components[0].Type != TextObjectComponentType.BeginText)
            {
                throw new InvalidOperationException("The set of components did not start with Begin Text (BT)");
            }

            if (components[components.Count - 1].Type != TextObjectComponentType.EndText)
            {
                throw new InvalidOperationException("The set of components did not end with End Text (ET)");
            }

            var builder = new TextObjectBuilder();

            for (var i = 1; i < components.Count - 1; i++)
            {
                if (components[i].IsOperator)
                {
                    ApplyOperator(builder, components, i, isLenientParsing);
                }
                else
                {
                    continue;
                }
            }

            return null;
        }

        private void ApplyOperator(TextObjectBuilder builder, IReadOnlyList<ITextObjectComponent> components, int index, bool isLenientParsing)
        {
            var current = components[index];

            if (!current.IsOperator)
            {
                throw new InvalidOperationException("Cannot apply operator for component type: " + current);
            }

            var operands = new IOperand[current.OperandTypes.Count];

            var start = index - operands.Length;

            // begin text or start
            if (start <= 0)
            {
                log.Error("Did not find the required number of operands for the current operator.");

                if (isLenientParsing)
                {
                    return;
                }

                throw new InvalidOperationException();
            }

            for (int i = start; i < index; i++)
            {
                var expectedOperand = current.OperandTypes[i - start];

                if (components[i].Type != expectedOperand)
                {
                    if (isLenientParsing)
                    {
                        return;
                    }

                    throw new InvalidOperationException($"Unexpected operand type at index {i - start} for operator: {current}\r\nExpected {expectedOperand} Found {components[i].Type}");                   
                }

                operands[i - start] = components[i].AsOperand;
            }
        }
    }

    public class TextObjectBuilder
    {
        public string FontKey { get; set; }

        public decimal FontSize { get; set; }
    }

    public interface ITextObjectComponent
    {
        bool IsOperator { get; }

        IReadOnlyList<TextObjectComponentType> OperandTypes { get; }

        TextObjectComponentType Type { get; }

        [CanBeNull]
        IOperand AsOperand { get; }
    }

    public interface IOperand
    {
        IReadOnlyList<byte> RawBytes { get; }
    }
}
