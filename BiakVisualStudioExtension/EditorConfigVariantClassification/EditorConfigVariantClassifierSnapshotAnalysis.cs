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
    private readonly object _analysisGate = new();
    private SnapshotAnalysis? _cachedSnapshotAnalysis;

    private SnapshotAnalysis GetSnapshotAnalysis(ITextSnapshot snapshot)
    {
        lock (_analysisGate)
        {
            if (_cachedSnapshotAnalysis is not null
                && ReferenceEquals(_cachedSnapshotAnalysis.Snapshot, snapshot))
            {
                return _cachedSnapshotAnalysis;
            }

            _cachedSnapshotAnalysis = BuildSnapshotAnalysis(snapshot);
            return _cachedSnapshotAnalysis;
        }
    }

    private void InvalidateSnapshotAnalysis()
    {
        lock (_analysisGate)
        {
            _cachedSnapshotAnalysis = null;
        }
    }

    private static SnapshotAnalysis BuildSnapshotAnalysis(ITextSnapshot snapshot)
    {
        HashSet<string> definedVariableNames = new(StringComparer.OrdinalIgnoreCase);
        Dictionary<int, string> validatedIncludeExcludeLineKinds = [];
        Dictionary<int, string> validatedAlwaysEnabledLineKinds = [];
        HashSet<int> biakVarExpressionContinuationLineNumbers = [];

        int pendingIncludeLineNumber = -1;
        int pendingExcludeLineNumber = -1;
        int pendingAlwaysEnabledStartLineNumber = -1;
        bool insideBiakVarExpression = false;

        for (int lineIndex = 0; lineIndex < snapshot.LineCount; lineIndex++)
        {
            string lineText = snapshot.GetLineFromLineNumber(lineIndex).GetText();

            if (insideBiakVarExpression)
            {
                biakVarExpressionContinuationLineNumbers.Add(lineIndex);
            }

            string? variableName = TryGetDefinedBiakVariableName(lineText);
            if (!string.IsNullOrEmpty(variableName))
            {
                definedVariableNames.Add(variableName!);
            }

            if (pendingExcludeLineNumber >= 0)
            {
                if (IsValidIncludeExcludeEndLine(lineText))
                {
                    validatedIncludeExcludeLineKinds[pendingIncludeLineNumber] = BiakDirectiveTokenConstant.INCLUDE;
                    validatedIncludeExcludeLineKinds[pendingExcludeLineNumber] = BiakDirectiveTokenConstant.EXCLUDE;
                    validatedIncludeExcludeLineKinds[lineIndex] = BiakSyntaxTokenConstant.INCLUDE_EXCLUDE_END;
                    pendingIncludeLineNumber = -1;
                    pendingExcludeLineNumber = -1;
                }
            }
            else if (pendingIncludeLineNumber >= 0)
            {
                if (!string.IsNullOrWhiteSpace(lineText))
                {
                    if (TryMatchIncludeExcludeDirectiveLine(lineText, BiakDirectiveTokenConstant.EXCLUDE))
                    {
                        pendingExcludeLineNumber = lineIndex;
                    }
                    else
                    {
                        pendingIncludeLineNumber = TryMatchIncludeExcludeDirectiveLine(lineText, BiakDirectiveTokenConstant.INCLUDE)
                            ? lineIndex
                            : -1;
                    }
                }
            }
            else if (TryMatchIncludeExcludeDirectiveLine(lineText, BiakDirectiveTokenConstant.INCLUDE))
            {
                pendingIncludeLineNumber = lineIndex;
            }

            if (pendingAlwaysEnabledStartLineNumber >= 0)
            {
                if (IsValidAlwaysEnabledBoundaryLine(lineText, BiakSyntaxTokenConstant.ALWAYS_ENABLED_END))
                {
                    validatedAlwaysEnabledLineKinds[pendingAlwaysEnabledStartLineNumber] = BiakSyntaxTokenConstant.ALWAYS_ENABLED_START;
                    validatedAlwaysEnabledLineKinds[lineIndex] = BiakSyntaxTokenConstant.ALWAYS_ENABLED_END;
                    pendingAlwaysEnabledStartLineNumber = -1;
                }
            }
            else if (IsValidAlwaysEnabledBoundaryLine(lineText, BiakSyntaxTokenConstant.ALWAYS_ENABLED_START))
            {
                pendingAlwaysEnabledStartLineNumber = lineIndex;
            }

            string trimmedStart = lineText.TrimStart();
            int indent = lineText.Length - trimmedStart.Length;
            bool looksLikeBiakVarContinuation = LooksLikeBiakVarContinuation(trimmedStart);

            if (string.IsNullOrWhiteSpace(lineText)
                || trimmedStart.StartsWith("#", StringComparison.Ordinal)
                || trimmedStart.StartsWith("[", StringComparison.Ordinal))
            {
                continue;
            }

            int equalsIndex = lineText.IndexOf('=');
            if (equalsIndex < 0)
            {
                if (!insideBiakVarExpression && !looksLikeBiakVarContinuation)
                {
                    continue;
                }

                int continuationStart = indent;
                int continuationCommentSearchStart = continuationStart;

                if (insideBiakVarExpression
                    && continuationStart < lineText.Length
                    && lineText[continuationStart] == ';')
                {
                    continuationCommentSearchStart = continuationStart + 1;
                }

                int continuationCommentStart = FindInlineCommentStart(
                    lineText,
                    continuationCommentSearchStart);
                int continuationEndExclusive = continuationCommentStart >= 0
                    ? continuationCommentStart
                    : lineText.Length;

                if (continuationEndExclusive > continuationStart)
                {
                    insideBiakVarExpression = !IsBiakVarExpressionTerminated(
                        lineText,
                        continuationStart,
                        continuationEndExclusive);
                }

                continue;
            }

            int valueStart = equalsIndex + 1;
            while (valueStart < lineText.Length && char.IsWhiteSpace(lineText[valueStart]))
            {
                valueStart++;
            }

            insideBiakVarExpression = false;

            if (valueStart >= lineText.Length)
            {
                continue;
            }

            if (string.IsNullOrEmpty(variableName))
            {
                continue;
            }

            int inlineCommentStart = FindInlineCommentStart(lineText, valueStart);
            int valueEndExclusive = inlineCommentStart >= 0
                ? inlineCommentStart
                : lineText.Length;

            if (valueEndExclusive > valueStart)
            {
                insideBiakVarExpression = !IsBiakVarExpressionTerminated(
                    lineText,
                    valueStart,
                    valueEndExclusive);
            }
        }

        return new SnapshotAnalysis(
            snapshot,
            definedVariableNames,
            validatedIncludeExcludeLineKinds,
            validatedAlwaysEnabledLineKinds,
            biakVarExpressionContinuationLineNumbers);
    }

    private sealed class SnapshotAnalysis
    {
        public SnapshotAnalysis(
            ITextSnapshot snapshot,
            HashSet<string> definedVariableNames,
            Dictionary<int, string> validatedIncludeExcludeLineKinds,
            Dictionary<int, string> validatedAlwaysEnabledLineKinds,
            HashSet<int> biakVarExpressionContinuationLineNumbers)
        {
            Snapshot = snapshot;
            DefinedVariableNames = definedVariableNames;
            ValidatedIncludeExcludeLineKinds = validatedIncludeExcludeLineKinds;
            ValidatedAlwaysEnabledLineKinds = validatedAlwaysEnabledLineKinds;
            BiakVarExpressionContinuationLineNumbers = biakVarExpressionContinuationLineNumbers;
        }

        public ITextSnapshot Snapshot { get; }

        public HashSet<string> DefinedVariableNames { get; }

        public Dictionary<int, string> ValidatedIncludeExcludeLineKinds { get; }

        public Dictionary<int, string> ValidatedAlwaysEnabledLineKinds { get; }

        public HashSet<int> BiakVarExpressionContinuationLineNumbers { get; }
    }
}
