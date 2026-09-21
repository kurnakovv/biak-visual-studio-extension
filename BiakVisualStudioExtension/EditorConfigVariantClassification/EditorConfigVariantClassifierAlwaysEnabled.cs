// Copyright (c) 2026 kurnakovv
// This file is licensed under the MIT License.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using BiakVisualStudioExtension.Constants;
using Microsoft.VisualStudio.Text;

namespace BiakVisualStudioExtension.EditorConfigVariantClassification;

internal sealed partial class EditorConfigVariantClassifier
{
    private static Dictionary<int, string> GetValidatedAlwaysEnabledLineKinds(
        ITextSnapshot snapshot)
    {
        Dictionary<int, string> lineKinds = [];

        for (int lineIndex = 0; lineIndex < snapshot.LineCount; lineIndex++)
        {
            if (!IsValidAlwaysEnabledBoundaryLine(snapshot.GetLineFromLineNumber(lineIndex).GetText(), BiakSyntaxTokenConstant.ALWAYS_ENABLED_START))
            {
                continue;
            }

            if (!TryGetAlwaysEnabledEndLineNumber(snapshot, lineIndex + 1, out int endLineNumber))
            {
                continue;
            }

            lineKinds[lineIndex] = BiakSyntaxTokenConstant.ALWAYS_ENABLED_START;
            lineKinds[endLineNumber] = BiakSyntaxTokenConstant.ALWAYS_ENABLED_END;
            lineIndex = endLineNumber;
        }

        return lineKinds;
    }

    private static bool TryGetAlwaysEnabledEndLineNumber(
        ITextSnapshot snapshot,
        int startLineNumber,
        out int lineNumber)
    {
        return TryFindLineNumber(
            snapshot,
            startLineNumber,
            lineText => IsValidAlwaysEnabledBoundaryLine(lineText, BiakSyntaxTokenConstant.ALWAYS_ENABLED_END),
            out lineNumber);
    }

    private static bool IsValidAlwaysEnabledBoundaryLine(
        string lineText,
        string expectedBoundary)
    {
        if (!TryGetBiakDirectiveToken(lineText, 0, out _, out string directiveToken, out int directiveTokenEnd)
            || directiveToken != BiakDirectiveTokenConstant.ALWAYS_ENABLED)
        {
            return false;
        }

        int boundaryStart = SkipWhitespace(lineText, directiveTokenEnd);
        if (!TryReadToken(
            lineText,
            boundaryStart,
            out string boundaryToken,
            out int boundaryEnd)
            || !boundaryToken.Equals(expectedBoundary, StringComparison.Ordinal))
        {
            return false;
        }

        for (int i = boundaryEnd; i < lineText.Length; i++)
        {
            if (!char.IsWhiteSpace(lineText[i]))
            {
                return false;
            }
        }

        return true;
    }

    private void TryAddStructuralTokenSpan(
        string lineText,
        int directiveEndExclusive,
        ICollection<BiakClassifiedSpan> spans,
        params string[] allowedTokens)
    {
        int tokenStart = SkipWhitespace(lineText, directiveEndExclusive);
        if (!TryReadToken(
            lineText,
            tokenStart,
            out string structuralToken,
            out _))
        {
            return;
        }

        foreach (string allowedToken in allowedTokens)
        {
            if (!structuralToken.Equals(allowedToken, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            spans.Add(new BiakClassifiedSpan(
                tokenStart,
                structuralToken.Length,
                _biakStructuralType));
            return;
        }
    }

    private bool TryAddAlwaysEnabledDirectiveSpans(
        string lineText,
        string directiveToken,
        int markerStart,
        int tokenStart,
        int tokenEnd,
        int tokenLength,
        bool hasValidatedAlwaysEnabledKind,
        string? alwaysEnabledLineKind,
        ICollection<BiakClassifiedSpan> spans)
    {
        if (directiveToken != BiakDirectiveTokenConstant.ALWAYS_ENABLED
            || !hasValidatedAlwaysEnabledKind)
        {
            return false;
        }

        spans.Add(new BiakClassifiedSpan(
            markerStart,
            BiakDirectiveTokenConstant.BIAK_MARKER_TOKEN.Length,
            _biakMarkerType));

        spans.Add(new BiakClassifiedSpan(
            tokenStart,
            tokenLength,
            _biakAlwaysEnabledType));

        if (string.Equals(alwaysEnabledLineKind, BiakSyntaxTokenConstant.ALWAYS_ENABLED_START, StringComparison.Ordinal))
        {
            TryAddStructuralTokenSpan(
                lineText,
                tokenEnd,
                spans,
                BiakSyntaxTokenConstant.ALWAYS_ENABLED_START);
        }
        else if (string.Equals(alwaysEnabledLineKind, BiakSyntaxTokenConstant.ALWAYS_ENABLED_END, StringComparison.Ordinal))
        {
            TryAddStructuralTokenSpan(
                lineText,
                tokenEnd,
                spans,
                BiakSyntaxTokenConstant.ALWAYS_ENABLED_END);
        }

        return true;
    }
}
