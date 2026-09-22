// Copyright (c) 2026 kurnakovv
// This file is licensed under the MIT License.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using BiakVisualStudioExtension.Constants;

namespace BiakVisualStudioExtension.EditorConfigVariantClassification;

internal sealed partial class EditorConfigVariantClassifier
{
    private static string? TryGetDefinedBiakVariableName(string lineText)
    {
        int searchStart = 0;

        while (searchStart < lineText.Length)
        {
            if (!TryGetBiakDirectiveToken(lineText, searchStart, out int markerStart, out string directiveToken, out int directiveTokenEnd))
            {
                return null;
            }

            if (directiveToken == BiakDirectiveTokenConstant.VAR)
            {
                int variableTokenStart = SkipWhitespace(lineText, directiveTokenEnd);
                if (TryReadToken(
                    lineText,
                    variableTokenStart,
                    out string variableName,
                    out _)
                    && IsValidBiakVariableName(variableName))
                {
                    return variableName;
                }
            }

            searchStart = markerStart + BiakDirectiveTokenConstant.BIAK_MARKER_TOKEN.Length;
        }

        return null;
    }

    private void AddBiakVariableReferenceSpans(
        string lineText,
        HashSet<string> definedVariableNames,
        ICollection<BiakClassifiedSpan> spans)
    {
        if (definedVariableNames.Count == 0)
        {
            return;
        }

        for (int i = 0; i < lineText.Length; i++)
        {
            if (lineText[i] != '$')
            {
                continue;
            }

            int variableNameStart = i + 1;
            if (variableNameStart >= lineText.Length
                || !IsBiakVariableNameStartCharacter(lineText[variableNameStart]))
            {
                continue;
            }

            int variableNameEnd = variableNameStart + 1;
            while (variableNameEnd < lineText.Length
                && IsBiakVariableNameCharacter(lineText[variableNameEnd]))
            {
                variableNameEnd++;
            }

            string variableName = lineText.Substring(
                variableNameStart,
                variableNameEnd - variableNameStart);
            if (!definedVariableNames.Contains(variableName))
            {
                i = variableNameEnd - 1;
                continue;
            }

            spans.Add(new BiakClassifiedSpan(
                i,
                variableNameEnd - i,
                _biakVariableNameType));
            i = variableNameEnd - 1;
        }
    }

    private static bool LooksLikeBiakVarContinuation(string trimmedStart)
    {
        return trimmedStart.StartsWith("+", StringComparison.Ordinal)
            || trimmedStart.StartsWith("\"", StringComparison.Ordinal)
            || trimmedStart.StartsWith(";", StringComparison.Ordinal);
    }

    private static bool IsBiakVarExpressionTerminated(
        string lineText,
        int startInclusive,
        int endExclusive)
    {
        bool insideString = false;

        for (int i = startInclusive; i < endExclusive; i++)
        {
            char currentChar = lineText[i];

            if (currentChar == '"' && !IsEscaped(lineText, i))
            {
                insideString = !insideString;
                continue;
            }

            if (!insideString && currentChar == ';')
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsValidBiakVariableName(string variableName)
    {
        if (string.IsNullOrEmpty(variableName)
            || !IsBiakVariableNameStartCharacter(variableName[0]))
        {
            return false;
        }

        for (int i = 1; i < variableName.Length; i++)
        {
            if (!IsBiakVariableNameCharacter(variableName[i]))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsBiakVariableNameStartCharacter(char ch)
    {
        return ch == '_' || char.IsLetter(ch);
    }

    private static bool IsBiakVariableNameCharacter(char ch)
    {
        return ch == '_' || char.IsLetterOrDigit(ch);
    }

    private void TryAddBiakVariableNameSpan(
        string lineText,
        int directiveEndExclusive,
        ICollection<BiakClassifiedSpan> spans)
    {
        int variableStart = SkipWhitespace(lineText, directiveEndExclusive);
        if (!TryReadToken(
            lineText,
            variableStart,
            out string variableName,
            out _)
            || !IsValidBiakVariableName(variableName))
        {
            return;
        }

        spans.Add(new BiakClassifiedSpan(
            variableStart,
            variableName.Length,
            _biakVariableNameType));
    }

    private bool HasBiakVarDirectiveToken(IReadOnlyList<BiakClassifiedSpan> biakSpans)
    {
        foreach (BiakClassifiedSpan span in biakSpans)
        {
            if (ReferenceEquals(span.ClassificationType, _biakVarType))
            {
                return true;
            }
        }

        return false;
    }

    private List<BiakClassifiedSpan> GetBiakVarExpressionSpans(
        string lineText,
        int startInclusive,
        int endExclusive,
        out bool hasExpressionTokens)
    {
        List<BiakClassifiedSpan> spans = [];
        bool insideString = false;
        int stringStart = -1;
        hasExpressionTokens = false;

        for (int i = startInclusive; i < endExclusive; i++)
        {
            char currentChar = lineText[i];

            if (currentChar == '"' && !IsEscaped(lineText, i))
            {
                if (!insideString)
                {
                    insideString = true;
                    stringStart = i;
                }
                else
                {
                    spans.Add(new BiakClassifiedSpan(
                        stringStart,
                        i - stringStart + 1,
                        _stringType));
                    insideString = false;
                    stringStart = -1;
                    hasExpressionTokens = true;
                }

                continue;
            }

            if (insideString)
            {
                continue;
            }

            if (currentChar == '+')
            {
                spans.Add(new BiakClassifiedSpan(i, 1, _operatorType));
                hasExpressionTokens = true;
                continue;
            }

            if (currentChar == ';')
            {
                spans.Add(new BiakClassifiedSpan(i, 1, _operatorType));
                hasExpressionTokens = true;
            }
        }

        return spans;
    }

    private bool TryAddVariableDirectiveSpans(
        string lineText,
        string directiveToken,
        int tokenStart,
        int tokenEnd,
        int tokenLength,
        ICollection<BiakClassifiedSpan> spans)
    {
        if (directiveToken != BiakDirectiveTokenConstant.VAR)
        {
            return false;
        }

        spans.Add(new BiakClassifiedSpan(
            tokenStart,
            tokenLength,
            _biakVarType));

        TryAddBiakVariableNameSpan(
            lineText,
            tokenEnd,
            spans);

        return true;
    }
}
