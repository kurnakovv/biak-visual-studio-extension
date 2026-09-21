// Copyright (c) 2026 kurnakovv
// This file is licensed under the MIT License.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using BiakVisualStudioExtension.Constants;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Tagging;

namespace BiakVisualStudioExtension.EditorConfigVariantClassification;

internal sealed partial class EditorConfigVariantClassifier : ITagger<ClassificationTag>
{
    private readonly IClassificationType _commentType;
    private readonly IClassificationType _keywordType;
    private readonly IClassificationType _biakMarkerType;
    private readonly IClassificationType _biakVarType;
    private readonly IClassificationType _biakImportType;
    private readonly IClassificationType _biakAlwaysEnabledType;
    private readonly IClassificationType _biakIncludeType;
    private readonly IClassificationType _biakExcludeType;
    private readonly IClassificationType _biakStructuralType;
    private readonly IClassificationType _biakVariableNameType;
    private readonly IClassificationType _biakBaselineType;
    private readonly IClassificationType _keyType;
    private readonly IClassificationType _operatorType;
    private readonly IClassificationType _stringType;
    private readonly IClassificationType _severityErrorType;
    private readonly IClassificationType _severityWarningType;
    private readonly IClassificationType _severitySuggestionType;
    private readonly IClassificationType _severityNoneType;
    private readonly IClassificationType _severitySilentType;

    public EditorConfigVariantClassifier(
        ITextBuffer textBuffer,
        IClassificationTypeRegistryService classificationTypeRegistryService)
    {
        if (textBuffer is null)
        {
            throw new ArgumentNullException(nameof(textBuffer));
        }

        if (classificationTypeRegistryService is null)
        {
            throw new ArgumentNullException(
                nameof(classificationTypeRegistryService));
        }

        textBuffer.Changed += OnTextBufferChanged;

        _commentType = classificationTypeRegistryService.GetClassificationType(
            PredefinedClassificationTypeNames.Comment);

        _keywordType = classificationTypeRegistryService.GetClassificationType(
            PredefinedClassificationTypeNames.Keyword);

        _biakMarkerType = classificationTypeRegistryService.GetClassificationType(
            EditorConfigVariantSeverityClassificationDefinitions.BIAK_MARKER_CLASSIFICATION_TYPE_NAME)
            ?? _keywordType;

        _biakVarType = classificationTypeRegistryService.GetClassificationType(
            EditorConfigVariantSeverityClassificationDefinitions.BIAK_VAR_CLASSIFICATION_TYPE_NAME)
            ?? _keywordType;

        _biakImportType = classificationTypeRegistryService.GetClassificationType(
            EditorConfigVariantSeverityClassificationDefinitions.BIAK_IMPORT_CLASSIFICATION_TYPE_NAME)
            ?? _keywordType;

        _biakAlwaysEnabledType = classificationTypeRegistryService.GetClassificationType(
            EditorConfigVariantSeverityClassificationDefinitions.BIAK_ALWAYS_ENABLED_CLASSIFICATION_TYPE_NAME)
            ?? _keywordType;

        _biakIncludeType = classificationTypeRegistryService.GetClassificationType(
            EditorConfigVariantSeverityClassificationDefinitions.BIAK_INCLUDE_CLASSIFICATION_TYPE_NAME)
            ?? _keywordType;

        _biakExcludeType = classificationTypeRegistryService.GetClassificationType(
            EditorConfigVariantSeverityClassificationDefinitions.BIAK_EXCLUDE_CLASSIFICATION_TYPE_NAME)
            ?? _keywordType;

        _biakStructuralType = classificationTypeRegistryService.GetClassificationType(
            EditorConfigVariantSeverityClassificationDefinitions.BIAK_STRUCTURAL_CLASSIFICATION_TYPE_NAME)
            ?? _keywordType;

        _biakBaselineType = classificationTypeRegistryService.GetClassificationType(
            EditorConfigVariantSeverityClassificationDefinitions.BIAK_BASELINE_CLASSIFICATION_TYPE_NAME)
            ?? _keywordType;

        IClassificationType identifierType = classificationTypeRegistryService.GetClassificationType(
            PredefinedClassificationTypeNames.Identifier);

        _keyType = classificationTypeRegistryService.GetClassificationType(
            EditorConfigVariantSeverityClassificationDefinitions.KEY_CLASSIFICATION_TYPE_NAME)
            ?? identifierType;

        _biakVariableNameType = classificationTypeRegistryService.GetClassificationType(
            EditorConfigVariantSeverityClassificationDefinitions.BIAK_VARIABLE_NAME_CLASSIFICATION_TYPE_NAME)
            ?? _keyType;

        _operatorType = classificationTypeRegistryService.GetClassificationType(
            PredefinedClassificationTypeNames.Operator);

        _stringType = classificationTypeRegistryService.GetClassificationType(
            PredefinedClassificationTypeNames.String);

        _severityErrorType =
            classificationTypeRegistryService.GetClassificationType(
                EditorConfigVariantSeverityClassificationDefinitions.ERROR_CLASSIFICATION_TYPE_NAME)
            ?? _stringType;

        _severityWarningType =
            classificationTypeRegistryService.GetClassificationType(
                EditorConfigVariantSeverityClassificationDefinitions.WARNING_CLASSIFICATION_TYPE_NAME)
            ?? _stringType;

        _severitySuggestionType =
            classificationTypeRegistryService.GetClassificationType(
                EditorConfigVariantSeverityClassificationDefinitions.SUGGESTION_CLASSIFICATION_TYPE_NAME)
            ?? _stringType;

        _severityNoneType =
            classificationTypeRegistryService.GetClassificationType(
                EditorConfigVariantSeverityClassificationDefinitions.NONE_CLASSIFICATION_TYPE_NAME)
            ?? _stringType;

        _severitySilentType =
            classificationTypeRegistryService.GetClassificationType(
                EditorConfigVariantSeverityClassificationDefinitions.SILENT_CLASSIFICATION_TYPE_NAME)
            ?? _stringType;
    }

    public event EventHandler<SnapshotSpanEventArgs>? TagsChanged;

    public IEnumerable<ITagSpan<ClassificationTag>> GetTags(
        NormalizedSnapshotSpanCollection spans)
    {
        if (spans.Count == 0)
        {
            yield break;
        }

        SnapshotAnalysis snapshotAnalysis = GetSnapshotAnalysis(spans[0].Snapshot);
        HashSet<string> definedVariableNames = snapshotAnalysis.DefinedVariableNames;
        Dictionary<int, string> validatedIncludeExcludeLineKinds = snapshotAnalysis.ValidatedIncludeExcludeLineKinds;
        Dictionary<int, string> validatedAlwaysEnabledLineKinds = snapshotAnalysis.ValidatedAlwaysEnabledLineKinds;

        int lastLineNumber = -1;
        bool insideBiakVarExpression = false;

        foreach (SnapshotSpan span in spans)
        {
            ITextSnapshotLine line = span.Start.GetContainingLine();

            if (line.LineNumber == lastLineNumber)
            {
                continue;
            }
            lastLineNumber = line.LineNumber;

            string lineText = line.GetText();

            if (string.IsNullOrWhiteSpace(lineText))
            {
                continue;
            }

            IReadOnlyList<BiakClassifiedSpan> biakSpans = GetBiakClassifiedSpans(
                lineText,
                line.LineNumber,
                definedVariableNames,
                validatedIncludeExcludeLineKinds,
                validatedAlwaysEnabledLineKinds);
            foreach (BiakClassifiedSpan biakSpan in biakSpans)
            {
                yield return CreateTagSpan(
                    line,
                    biakSpan.StartOffset,
                    biakSpan.Length,
                    biakSpan.ClassificationType);
            }

            string trimmedStart = lineText.TrimStart();
            int indent = lineText.Length - trimmedStart.Length;
            bool looksLikeBiakVarContinuation = LooksLikeBiakVarContinuation(trimmedStart);

            if (trimmedStart.StartsWith("#", StringComparison.Ordinal)
                || (trimmedStart.StartsWith(";", StringComparison.Ordinal)
                    && !insideBiakVarExpression
                    && !looksLikeBiakVarContinuation))
            {
                IEnumerable<TagSpan<ClassificationTag>> commentSpans = CreateTagSpansExcludingBiak(
                    line,
                    indent,
                    trimmedStart.Length,
                    _commentType,
                    biakSpans
                );
                foreach (TagSpan<ClassificationTag> commentSpan in commentSpans)
                {
                    yield return commentSpan;
                }

                continue;
            }

            if (trimmedStart.StartsWith("[", StringComparison.Ordinal))
            {
                int sectionEndInTrimmed = trimmedStart.IndexOf(']');
                if (sectionEndInTrimmed >= 0)
                {
                    int sectionLength = sectionEndInTrimmed + 1;
                    IEnumerable<TagSpan<ClassificationTag>> sectionSpans = CreateTagSpansExcludingBiak(
                        line,
                        indent,
                        sectionLength,
                        _keywordType,
                        biakSpans
                    );
                    foreach (TagSpan<ClassificationTag> sectionSpan in sectionSpans)
                    {
                        yield return sectionSpan;
                    }

                    int sectionEndInLine = indent + sectionLength;
                    int sectionCommentStart = FindInlineCommentStart(
                        lineText,
                        sectionEndInLine);
                    if (sectionCommentStart >= 0)
                    {
                        IEnumerable<TagSpan<ClassificationTag>> commentSpans = CreateTagSpansExcludingBiak(
                            line,
                            sectionCommentStart,
                            lineText.Length - sectionCommentStart,
                            _commentType,
                            biakSpans
                        );
                        foreach (TagSpan<ClassificationTag> commentSpan in commentSpans)
                        {
                            yield return commentSpan;
                        }
                    }

                    continue;
                }
            }

            int equalsIndex = lineText.IndexOf('=');
            if (equalsIndex < 0)
            {
                if (TryGetBiakImportStringBounds(lineText, out int importStringStart, out int importStringLength, out int importCommentStart))
                {
                    yield return CreateTagSpan(
                        line,
                        importStringStart,
                        importStringLength,
                        _stringType);

                    if (importCommentStart >= 0)
                    {
                        IEnumerable<TagSpan<ClassificationTag>> commentSpans = CreateTagSpansExcludingBiak(
                            line,
                            importCommentStart,
                            lineText.Length - importCommentStart,
                            _commentType,
                            biakSpans
                        );
                        foreach (TagSpan<ClassificationTag> commentSpan in commentSpans)
                        {
                            yield return commentSpan;
                        }
                    }

                    continue;
                }

                if (!insideBiakVarExpression && !looksLikeBiakVarContinuation)
                {
                    continue;
                }

                int continuationStart = indent;
                int continuationCommentStart = FindInlineCommentStart(
                    lineText,
                    continuationStart);
                int continuationEndExclusive = continuationCommentStart >= 0
                    ? continuationCommentStart
                    : lineText.Length;

                if (continuationEndExclusive > continuationStart)
                {
                    List<BiakClassifiedSpan> continuationExpressionSpans =
                        GetBiakVarExpressionSpans(
                            lineText,
                            continuationStart,
                            continuationEndExclusive,
                            out bool isContinuationTerminated,
                            out bool hasContinuationTokens);

                    if (hasContinuationTokens)
                    {
                        foreach (BiakClassifiedSpan expressionSpan in continuationExpressionSpans)
                        {
                            yield return CreateTagSpan(
                                line,
                                expressionSpan.StartOffset,
                                expressionSpan.Length,
                                expressionSpan.ClassificationType);
                        }
                    }

                    insideBiakVarExpression = !isContinuationTerminated;
                }

                if (continuationCommentStart >= 0)
                {
                    IEnumerable<TagSpan<ClassificationTag>> commentSpans = CreateTagSpansExcludingBiak(
                        line,
                        continuationCommentStart,
                        lineText.Length - continuationCommentStart,
                        _commentType,
                        biakSpans
                    );
                    foreach (TagSpan<ClassificationTag> commentSpan in commentSpans)
                    {
                        yield return commentSpan;
                    }
                }

                continue;
            }

            int keyStart = 0;
            while (keyStart < equalsIndex && char.IsWhiteSpace(lineText[keyStart]))
            {
                keyStart++;
            }

            int keyEnd = equalsIndex - 1;
            while (keyEnd >= keyStart && char.IsWhiteSpace(lineText[keyEnd]))
            {
                keyEnd--;
            }

            if (keyEnd >= keyStart)
            {
                IEnumerable<TagSpan<ClassificationTag>> keySpans = CreateTagSpansExcludingBiak(
                    line,
                    keyStart,
                    keyEnd - keyStart + 1,
                    _keyType,
                    biakSpans
                );
                foreach (TagSpan<ClassificationTag> keySpan in keySpans)
                {
                    yield return keySpan;
                }
            }

            yield return CreateTagSpan(
                line,
                equalsIndex,
                1,
                _operatorType);

            int valueStart = equalsIndex + 1;
            while (valueStart < lineText.Length && char.IsWhiteSpace(lineText[valueStart]))
            {
                valueStart++;
            }

            if (valueStart >= lineText.Length)
            {
                continue;
            }

            int inlineCommentStart = FindInlineCommentStart(lineText, valueStart);
            int valueEndExclusive = inlineCommentStart >= 0
                ? inlineCommentStart
                : lineText.Length;

            string keyText = keyEnd >= keyStart
                 ? lineText.Substring(keyStart, keyEnd - keyStart + 1)
                 : string.Empty;

            bool isBiakVarAssignmentLine = HasBiakVarDirectiveToken(biakSpans);
            insideBiakVarExpression = false;

            if (valueEndExclusive > valueStart)
            {
                if (IsSeverityKey(keyText)
                    && TryGetTrimmedValueBounds(
                        lineText,
                        valueStart,
                        valueEndExclusive,
                        out int severityStart,
                        out int severityLength))
                {
                    string severityValue = lineText.Substring(
                        severityStart,
                        severityLength);

                    yield return CreateTagSpan(
                        line,
                        severityStart,
                        severityLength,
                        GetSeverityClassificationType(severityValue));
                }
                else if (isBiakVarAssignmentLine)
                {
                    List<BiakClassifiedSpan> expressionSpans = GetBiakVarExpressionSpans(
                        lineText,
                        valueStart,
                        valueEndExclusive,
                        out bool isTerminated,
                        out bool hasExpressionTokens);

                    if (hasExpressionTokens)
                    {
                        foreach (BiakClassifiedSpan expressionSpan in expressionSpans)
                        {
                            yield return CreateTagSpan(
                                line,
                                expressionSpan.StartOffset,
                                expressionSpan.Length,
                                expressionSpan.ClassificationType);
                        }
                    }
                    else
                    {
                        yield return CreateTagSpan(
                            line,
                            valueStart,
                            valueEndExclusive - valueStart,
                            _stringType);
                    }

                    insideBiakVarExpression = !isTerminated;
                }
                else
                {
                    IEnumerable<TagSpan<ClassificationTag>> valueSpans = CreateTagSpansExcludingBiak(
                        line,
                        valueStart,
                        valueEndExclusive - valueStart,
                        _stringType,
                        biakSpans
                    );
                    foreach (TagSpan<ClassificationTag> valueSpan in valueSpans)
                    {
                        yield return valueSpan;
                    }
                }
            }

            if (inlineCommentStart >= 0)
            {
                IEnumerable<TagSpan<ClassificationTag>> commentSpans = CreateTagSpansExcludingBiak(
                    line,
                    inlineCommentStart,
                    lineText.Length - inlineCommentStart,
                    _commentType,
                    biakSpans
                );
                foreach (TagSpan<ClassificationTag> commentSpan in commentSpans)
                {
                    yield return commentSpan;
                }
            }
        }
    }

    private void OnTextBufferChanged(
        object? sender,
        TextContentChangedEventArgs e)
    {
        if (e.Changes.Count == 0)
        {
            return;
        }

        InvalidateSnapshotAnalysis();

        SnapshotSpan fullSnapshotSpan = new(e.After, 0, e.After.Length);
        TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(fullSnapshotSpan));
    }

    private IClassificationType GetSeverityClassificationType(
        string severityValue)
    {
        return severityValue.ToLowerInvariant() switch
        {
            "error" => _severityErrorType,
            "warning" => _severityWarningType,
            "suggestion" => _severitySuggestionType,
            "none" => _severityNoneType,
            "silent" or "hidden" => _severitySilentType,
            _ => _stringType,
        };
    }

    private static bool IsSeverityKey(string keyText)
    {
        return keyText.EndsWith(".severity", StringComparison.OrdinalIgnoreCase)
            || (keyText.StartsWith("resharper_", StringComparison.OrdinalIgnoreCase)
                && keyText.EndsWith("_highlighting", StringComparison.OrdinalIgnoreCase));
    }

    private List<BiakClassifiedSpan> GetBiakClassifiedSpans(
        string lineText,
        int lineNumber,
        HashSet<string> definedVariableNames,
        IReadOnlyDictionary<int, string> validatedIncludeExcludeLineKinds,
        IReadOnlyDictionary<int, string> validatedAlwaysEnabledLineKinds)
    {
        List<BiakClassifiedSpan> spans = [];
        int searchStart = 0;

        AddBiakVariableReferenceSpans(
            lineText,
            definedVariableNames,
            spans);

        while (searchStart < lineText.Length)
        {
            int markerStart = lineText.IndexOf(
                BiakDirectiveTokenConstant.BIAK_MARKER_TOKEN,
                searchStart,
                StringComparison.Ordinal);

            if (markerStart < 0)
            {
                break;
            }

            int tokenStart = markerStart + BiakDirectiveTokenConstant.BIAK_MARKER_TOKEN.Length;
            while (tokenStart < lineText.Length
                && char.IsWhiteSpace(lineText[tokenStart]))
            {
                tokenStart++;
            }

            if (tokenStart < lineText.Length)
            {
                int tokenEnd = tokenStart;
                while (tokenEnd < lineText.Length
                    && !char.IsWhiteSpace(lineText[tokenEnd]))
                {
                    tokenEnd++;
                }

                int tokenLength = tokenEnd - tokenStart;
                if (tokenLength > 0)
                {
                    string directiveToken = lineText.Substring(tokenStart, tokenLength);
                    bool hasValidatedIncludeExcludeKind =
                        validatedIncludeExcludeLineKinds.TryGetValue(
                            lineNumber,
                            out string? includeExcludeLineKind);
                    bool hasValidatedAlwaysEnabledKind =
                        validatedAlwaysEnabledLineKinds.TryGetValue(
                            lineNumber,
                            out string? alwaysEnabledLineKind);

                    if (!TryAddVariableDirectiveSpans(
                        lineText,
                        directiveToken,
                        markerStart,
                        tokenStart,
                        tokenEnd,
                        tokenLength,
                        spans)
                        && !TryAddImportDirectiveSpans(
                            directiveToken,
                            markerStart,
                            tokenStart,
                            tokenLength,
                            spans)
                        && !TryAddAlwaysEnabledDirectiveSpans(
                            lineText,
                            directiveToken,
                            markerStart,
                            tokenStart,
                            tokenEnd,
                            tokenLength,
                            hasValidatedAlwaysEnabledKind,
                            alwaysEnabledLineKind,
                            spans)
                        && !TryAddIncludeExcludeDirectiveSpans(
                            lineText,
                            directiveToken,
                            markerStart,
                            tokenStart,
                            tokenEnd,
                            tokenLength,
                            hasValidatedIncludeExcludeKind,
                            includeExcludeLineKind,
                            spans))
                    {
                        TryAddBaselineDirectiveSpans(
                            directiveToken,
                            markerStart,
                            tokenStart,
                            tokenLength,
                            spans);
                    }
                }
            }

            searchStart = markerStart + BiakDirectiveTokenConstant.BIAK_MARKER_TOKEN.Length;
        }

        spans.Sort(static (left, right) => left.StartOffset.CompareTo(right.StartOffset));
        return spans;
    }
}
