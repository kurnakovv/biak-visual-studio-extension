// Copyright (c) 2026 kurnakovv
// This file is licensed under the MIT License.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using BiakVisualStudioExtension.Constants;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Tagging;

namespace BiakVisualStudioExtension.EditorConfigVariantClassification;

internal sealed partial class EditorConfigVariantClassifier
{
    private static bool TryFindLineNumber(
        ITextSnapshot snapshot,
        int startLineNumber,
        Func<string, bool> predicate,
        out int lineNumber)
    {
        for (int currentLineNumber = startLineNumber; currentLineNumber < snapshot.LineCount; currentLineNumber++)
        {
            string lineText = snapshot.GetLineFromLineNumber(currentLineNumber).GetText();
            if (predicate(lineText))
            {
                lineNumber = currentLineNumber;
                return true;
            }
        }

        lineNumber = -1;
        return false;
    }

    private static bool TryGetBiakDirectiveToken(
        string lineText,
        int searchStart,
        out int markerStart,
        out string directiveToken,
        out int directiveTokenEnd)
    {
        markerStart = lineText.IndexOf(
            BiakDirectiveTokenConstant.BIAK_MARKER_TOKEN,
            searchStart,
            StringComparison.Ordinal);
        directiveToken = string.Empty;
        directiveTokenEnd = 0;

        if (markerStart < 0)
        {
            return false;
        }

        int directiveTokenStart = SkipWhitespace(
            lineText,
            markerStart + BiakDirectiveTokenConstant.BIAK_MARKER_TOKEN.Length);
        return TryReadToken(
            lineText,
            directiveTokenStart,
            out directiveToken,
            out directiveTokenEnd);
    }

    private static int SkipWhitespace(string text, int startIndex)
    {
        int index = startIndex;
        while (index < text.Length && char.IsWhiteSpace(text[index]))
        {
            index++;
        }

        return index;
    }

    private static bool TryReadToken(
        string text,
        int startIndex,
        out string token,
        out int tokenEndExclusive)
    {
        token = string.Empty;
        tokenEndExclusive = startIndex;

        if (startIndex >= text.Length || text[startIndex] is '=' or '#' or ';')
        {
            return false;
        }

        int index = startIndex;
        while (index < text.Length
            && !char.IsWhiteSpace(text[index])
            && text[index] is not ('=' or '#' or ';'))
        {
            index++;
        }

        if (index <= startIndex)
        {
            return false;
        }

        token = text.Substring(startIndex, index - startIndex);
        tokenEndExclusive = index;
        return true;
    }

    private static bool IsEscaped(string text, int charIndex)
    {
        int slashCount = 0;

        for (int i = charIndex - 1; i >= 0 && text[i] == '\\'; i--)
        {
            slashCount++;
        }

        return slashCount % 2 == 1;
    }

    private static IEnumerable<TagSpan<ClassificationTag>> CreateTagSpansExcludingBiak(
        ITextSnapshotLine line,
        int spanStart,
        int spanLength,
        IClassificationType classificationType,
        IReadOnlyList<BiakClassifiedSpan> biakSpans)
    {
        int spanEndExclusive = spanStart + spanLength;
        int currentStart = spanStart;

        foreach (BiakClassifiedSpan biakSpan in biakSpans)
        {
            int biakStart = biakSpan.StartOffset;
            int biakEndExclusive = biakSpan.StartOffset + biakSpan.Length;

            if (biakEndExclusive <= spanStart)
            {
                continue;
            }

            if (biakStart >= spanEndExclusive)
            {
                break;
            }

            int clippedBiakStart = Math.Max(spanStart, biakStart);
            int clippedBiakEndExclusive = Math.Min(spanEndExclusive, biakEndExclusive);

            if (clippedBiakStart > currentStart)
            {
                yield return CreateTagSpan(
                    line,
                    currentStart,
                    clippedBiakStart - currentStart,
                    classificationType);
            }

            if (clippedBiakEndExclusive > currentStart)
            {
                currentStart = clippedBiakEndExclusive;
            }
        }

        if (currentStart < spanEndExclusive)
        {
            yield return CreateTagSpan(
                line,
                currentStart,
                spanEndExclusive - currentStart,
                classificationType);
        }
    }

    private static bool TryGetTrimmedValueBounds(
        string lineText,
        int startInclusive,
        int endExclusive,
        out int trimmedStart,
        out int trimmedLength)
    {
        trimmedStart = startInclusive;
        int trimmedEnd = endExclusive - 1;

        while (trimmedStart < endExclusive
            && char.IsWhiteSpace(lineText[trimmedStart]))
        {
            trimmedStart++;
        }

        while (trimmedEnd >= trimmedStart
            && char.IsWhiteSpace(lineText[trimmedEnd]))
        {
            trimmedEnd--;
        }

        trimmedLength = trimmedEnd - trimmedStart + 1;
        return trimmedLength > 0;
    }

    private static int FindInlineCommentStart(
        string lineText,
        int startIndex)
    {
        for (int i = startIndex; i < lineText.Length; i++)
        {
            char currentChar = lineText[i];
            if (currentChar is not ('#' or ';'))
            {
                continue;
            }

            if (i == startIndex || char.IsWhiteSpace(lineText[i - 1]))
            {
                return i;
            }
        }

        return -1;
    }

    private static TagSpan<ClassificationTag> CreateTagSpan(
        ITextSnapshotLine line,
        int startOffset,
        int length,
        IClassificationType classificationType)
    {
        SnapshotPoint spanStart = line.Start + startOffset;
        SnapshotSpan snapshotSpan = new(spanStart, length);
        ClassificationTag tag = new(classificationType);
        return new TagSpan<ClassificationTag>(snapshotSpan, tag);
    }

    private readonly struct BiakClassifiedSpan
    {
        public BiakClassifiedSpan(
            int startOffset,
            int length,
            IClassificationType classificationType)
        {
            StartOffset = startOffset;
            Length = length;
            ClassificationType = classificationType;
        }

        public int StartOffset { get; }

        public int Length { get; }

        public IClassificationType ClassificationType { get; }
    }
}
