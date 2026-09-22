// Copyright (c) 2026 kurnakovv
// This file is licensed under the MIT License.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using BiakVisualStudioExtension.Constants;

namespace BiakVisualStudioExtension.EditorConfigVariantClassification;

internal sealed partial class EditorConfigVariantClassifier
{
    private static bool TryMatchIncludeExcludeDirectiveLine(
        string lineText,
        string expectedDirective)
    {
        if (!TryGetBiakDirectiveToken(lineText, 0, out _, out string directiveToken, out int directiveTokenEnd)
            || !directiveToken.Equals(expectedDirective, StringComparison.Ordinal))
        {
            return false;
        }

        int bracketStart = SkipWhitespace(lineText, directiveTokenEnd);
        if (bracketStart >= lineText.Length || lineText[bracketStart] != '[')
        {
            return false;
        }

        int closingBracket = lineText.LastIndexOf(']');
        if (closingBracket <= bracketStart)
        {
            return false;
        }

        for (int i = closingBracket + 1; i < lineText.Length; i++)
        {
            if (!char.IsWhiteSpace(lineText[i]))
            {
                return false;
            }
        }

        string bracketContent = lineText.Substring(
            bracketStart + 1,
            closingBracket - bracketStart - 1);
        return !string.IsNullOrWhiteSpace(bracketContent);
    }

    private static bool IsValidIncludeExcludeEndLine(string lineText)
    {
        if (!TryGetBiakDirectiveToken(lineText, 0, out _, out string directiveToken, out int directiveTokenEnd)
            || !directiveToken.Equals(BiakSyntaxTokenConstant.INCLUDE_EXCLUDE_END, StringComparison.Ordinal))
        {
            return false;
        }

        int pairStart = SkipWhitespace(lineText, directiveTokenEnd);
        if (lineText.IndexOf(BiakDirectiveTokenConstant.INCLUDE_EXCLUDE_PAIR, pairStart, StringComparison.Ordinal) != pairStart)
        {
            return false;
        }

        for (int i = pairStart + BiakDirectiveTokenConstant.INCLUDE_EXCLUDE_PAIR.Length; i < lineText.Length; i++)
        {
            if (!char.IsWhiteSpace(lineText[i]))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsBiakStructuralToken(string directiveToken)
    {
        return directiveToken.Equals(BiakSyntaxTokenConstant.INCLUDE_EXCLUDE_END, StringComparison.OrdinalIgnoreCase);
    }

    private void TryAddIncludeExcludePairSpan(
        string lineText,
        int directiveEndExclusive,
        ICollection<BiakClassifiedSpan> spans)
    {
        int tokenStart = SkipWhitespace(lineText, directiveEndExclusive);
        if (tokenStart >= lineText.Length)
        {
            return;
        }

        if (lineText.IndexOf(BiakDirectiveTokenConstant.INCLUDE_EXCLUDE_PAIR, tokenStart, StringComparison.OrdinalIgnoreCase) != tokenStart)
        {
            return;
        }

        spans.Add(new BiakClassifiedSpan(
            tokenStart,
            BiakDirectiveTokenConstant.INCLUDE.Length,
            _biakIncludeType));

        spans.Add(new BiakClassifiedSpan(
            tokenStart + BiakDirectiveTokenConstant.INCLUDE.Length + 1,
            BiakDirectiveTokenConstant.EXCLUDE.Length,
            _biakExcludeType));
    }

    private void TryAddBracketedStringSpan(
        string lineText,
        int directiveEndExclusive,
        ICollection<BiakClassifiedSpan> spans)
    {
        int bracketStart = SkipWhitespace(lineText, directiveEndExclusive);
        if (bracketStart >= lineText.Length || lineText[bracketStart] != '[')
        {
            return;
        }

        int closingBracket = lineText.LastIndexOf(']');
        if (closingBracket <= bracketStart)
        {
            return;
        }

        spans.Add(new BiakClassifiedSpan(
            bracketStart,
            closingBracket - bracketStart + 1,
            _keywordType));
    }

    private bool TryAddIncludeExcludeDirectiveSpans(
        string lineText,
        string directiveToken,
        int tokenStart,
        int tokenEnd,
        int tokenLength,
        bool hasValidatedIncludeExcludeKind,
        string? includeExcludeLineKind,
        ICollection<BiakClassifiedSpan> spans)
    {
        if (directiveToken == BiakDirectiveTokenConstant.INCLUDE
            && hasValidatedIncludeExcludeKind
            && includeExcludeLineKind == BiakDirectiveTokenConstant.INCLUDE)
        {
            spans.Add(new BiakClassifiedSpan(
                tokenStart,
                tokenLength,
                _biakIncludeType));

            TryAddBracketedStringSpan(
                lineText,
                tokenEnd,
                spans);
            return true;
        }

        if (directiveToken == BiakDirectiveTokenConstant.EXCLUDE
            && hasValidatedIncludeExcludeKind
            && includeExcludeLineKind == BiakDirectiveTokenConstant.EXCLUDE)
        {
            spans.Add(new BiakClassifiedSpan(
                tokenStart,
                tokenLength,
                _biakExcludeType));

            TryAddBracketedStringSpan(
                lineText,
                tokenEnd,
                spans);
            return true;
        }

        if (IsBiakStructuralToken(directiveToken)
            && hasValidatedIncludeExcludeKind
            && string.Equals(includeExcludeLineKind, BiakSyntaxTokenConstant.INCLUDE_EXCLUDE_END, StringComparison.Ordinal))
        {
            spans.Add(new BiakClassifiedSpan(
                tokenStart,
                tokenLength,
                _biakStructuralType));

            TryAddIncludeExcludePairSpan(
                lineText,
                tokenEnd,
                spans);
            return true;
        }

        return false;
    }
}
