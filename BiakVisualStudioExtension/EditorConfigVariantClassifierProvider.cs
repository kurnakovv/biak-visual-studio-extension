// Copyright (c) 2026 kurnakovv
// This file is licensed under the MIT License.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace BiakVisualStudioExtension;

[Export(typeof(ITaggerProvider))]
[ContentType(EditorConfigVariantContentTypeDefinition.CONTENT_TYPE_NAME)]
[TagType(typeof(ClassificationTag))]
internal sealed class EditorConfigVariantClassifierProvider : ITaggerProvider
{
    [Import]
    internal IClassificationTypeRegistryService ClassificationTypeRegistryService { get; set; } = null!;

    public ITagger<T>? CreateTagger<T>(ITextBuffer buffer)
        where T : ITag
    {
        if (buffer is null)
        {
            throw new ArgumentNullException(nameof(buffer));
        }

        return buffer.Properties.GetOrCreateSingletonProperty(
            () => new EditorConfigVariantClassifier(
                buffer,
                ClassificationTypeRegistryService)) as ITagger<T>;
    }
}

internal sealed class EditorConfigVariantClassifier : ITagger<ClassificationTag>
{
    private const string BIAK_MARKER_TOKEN = "^biak^";

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

        HashSet<string> definedVariableNames = GetDefinedBiakVariableNames(spans[0].Snapshot);
        Dictionary<int, string> validatedIncludeExcludeLineKinds = GetValidatedIncludeExcludeLineKinds(spans[0].Snapshot);
        Dictionary<int, string> validatedAlwaysEnabledLineKinds = GetValidatedAlwaysEnabledLineKinds(spans[0].Snapshot);

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
                IEnumerable<TagSpan<ClassificationTag>> commentSpans = CreateCommentTagSpansExcludingBiak(
                    line,
                    indent,
                    trimmedStart.Length,
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
                        IEnumerable<TagSpan<ClassificationTag>> commentSpans = CreateCommentTagSpansExcludingBiak(
                            line,
                            sectionCommentStart,
                            lineText.Length - sectionCommentStart,
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
                        IEnumerable<TagSpan<ClassificationTag>> commentSpans = CreateCommentTagSpansExcludingBiak(
                            line,
                            importCommentStart,
                            lineText.Length - importCommentStart,
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
                    IEnumerable<TagSpan<ClassificationTag>> commentSpans = CreateCommentTagSpansExcludingBiak(
                        line,
                        continuationCommentStart,
                        lineText.Length - continuationCommentStart,
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
                IEnumerable<TagSpan<ClassificationTag>> commentSpans = CreateCommentTagSpansExcludingBiak(
                    line,
                    inlineCommentStart,
                    lineText.Length - inlineCommentStart,
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
                BIAK_MARKER_TOKEN,
                searchStart,
                StringComparison.Ordinal);

            if (markerStart < 0)
            {
                break;
            }

            int tokenStart = markerStart + BIAK_MARKER_TOKEN.Length;
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

                    if (IsBiakVarToken(directiveToken))
                    {
                        spans.Add(new BiakClassifiedSpan(
                            markerStart,
                            BIAK_MARKER_TOKEN.Length,
                            _biakMarkerType));

                        spans.Add(new BiakClassifiedSpan(
                            tokenStart,
                            tokenLength,
                            _biakVarType));

                        TryAddBiakVariableNameSpan(
                            lineText,
                            tokenEnd,
                            spans);
                    }
                    else if (IsBiakImportToken(directiveToken))
                    {
                        spans.Add(new BiakClassifiedSpan(
                            markerStart,
                            BIAK_MARKER_TOKEN.Length,
                            _biakMarkerType));

                        spans.Add(new BiakClassifiedSpan(
                            tokenStart,
                            tokenLength,
                            _biakImportType));
                    }
                    else if (IsBiakAlwaysEnabledToken(directiveToken)
                             && hasValidatedAlwaysEnabledKind)
                    {
                        spans.Add(new BiakClassifiedSpan(
                            markerStart,
                            BIAK_MARKER_TOKEN.Length,
                            _biakMarkerType));

                        spans.Add(new BiakClassifiedSpan(
                            tokenStart,
                            tokenLength,
                            _biakAlwaysEnabledType));

                        if (string.Equals(alwaysEnabledLineKind, "start", StringComparison.Ordinal))
                        {
                            TryAddStructuralTokenSpan(
                                lineText,
                                tokenEnd,
                                spans,
                                "start");
                        }
                        else if (string.Equals(alwaysEnabledLineKind, "end", StringComparison.Ordinal))
                        {
                            TryAddStructuralTokenSpan(
                                lineText,
                                tokenEnd,
                                spans,
                                "end");
                        }
                    }
                    else if (IsBiakIncludeToken(directiveToken)
                             && hasValidatedIncludeExcludeKind
                             && string.Equals(includeExcludeLineKind, "include", StringComparison.Ordinal))
                    {
                        spans.Add(new BiakClassifiedSpan(
                            markerStart,
                            BIAK_MARKER_TOKEN.Length,
                            _biakMarkerType));

                        spans.Add(new BiakClassifiedSpan(
                            tokenStart,
                            tokenLength,
                            _biakIncludeType));

                        TryAddBracketedStringSpan(
                            lineText,
                            tokenEnd,
                            spans);
                    }
                    else if (IsBiakExcludeToken(directiveToken)
                             && hasValidatedIncludeExcludeKind
                             && string.Equals(includeExcludeLineKind, "exclude", StringComparison.Ordinal))
                    {
                        spans.Add(new BiakClassifiedSpan(
                            markerStart,
                            BIAK_MARKER_TOKEN.Length,
                            _biakMarkerType));

                        spans.Add(new BiakClassifiedSpan(
                            tokenStart,
                            tokenLength,
                            _biakExcludeType));

                        TryAddBracketedStringSpan(
                            lineText,
                            tokenEnd,
                            spans);
                    }
                    else if (IsBiakStructuralToken(directiveToken)
                             && hasValidatedIncludeExcludeKind
                             && string.Equals(includeExcludeLineKind, "END", StringComparison.Ordinal))
                    {
                        spans.Add(new BiakClassifiedSpan(
                            markerStart,
                            BIAK_MARKER_TOKEN.Length,
                            _biakMarkerType));

                        spans.Add(new BiakClassifiedSpan(
                            tokenStart,
                            tokenLength,
                            _biakStructuralType));

                        TryAddIncludeExcludePairSpan(
                            lineText,
                            tokenEnd,
                            spans);
                    }
                    else if (IsBiakBaselineToken(directiveToken))
                    {
                        spans.Add(new BiakClassifiedSpan(
                            markerStart,
                            BIAK_MARKER_TOKEN.Length,
                            _biakMarkerType));

                        spans.Add(new BiakClassifiedSpan(
                            tokenStart,
                            tokenLength,
                            _biakBaselineType));
                    }
                }
            }

            searchStart = markerStart + BIAK_MARKER_TOKEN.Length;
        }

        spans.Sort(static (left, right) => left.StartOffset.CompareTo(right.StartOffset));
        return spans;
    }

    private HashSet<string> GetDefinedBiakVariableNames(ITextSnapshot snapshot)
    {
        HashSet<string> variableNames = new(StringComparer.OrdinalIgnoreCase);

        foreach (ITextSnapshotLine line in snapshot.Lines)
        {
            string? variableName = TryGetDefinedBiakVariableName(line.GetText());
            if (!string.IsNullOrEmpty(variableName))
            {
                variableNames.Add(variableName!);
            }
        }

        return variableNames;
    }

    private static Dictionary<int, string> GetValidatedIncludeExcludeLineKinds(
        ITextSnapshot snapshot)
    {
        Dictionary<int, string> lineKinds = [];

        for (int lineIndex = 0; lineIndex < snapshot.LineCount; lineIndex++)
        {
            if (!TryMatchIncludeExcludeDirectiveLine(snapshot.GetLineFromLineNumber(lineIndex).GetText(), "include"))
            {
                continue;
            }

            if (!TryGetNextNonBlankLineNumber(snapshot, lineIndex + 1, out int excludeLineNumber)
                || !TryMatchIncludeExcludeDirectiveLine(snapshot.GetLineFromLineNumber(excludeLineNumber).GetText(), "exclude")
                || !TryGetIncludeExcludeEndLineNumber(snapshot, excludeLineNumber + 1, out int endLineNumber))
            {
                continue;
            }

            lineKinds[lineIndex] = "include";
            lineKinds[excludeLineNumber] = "exclude";
            lineKinds[endLineNumber] = "END";
            lineIndex = endLineNumber;
        }

        return lineKinds;
    }

    private static Dictionary<int, string> GetValidatedAlwaysEnabledLineKinds(
        ITextSnapshot snapshot)
    {
        Dictionary<int, string> lineKinds = [];

        for (int lineIndex = 0; lineIndex < snapshot.LineCount; lineIndex++)
        {
            if (!IsValidAlwaysEnabledBoundaryLine(snapshot.GetLineFromLineNumber(lineIndex).GetText(), "start"))
            {
                continue;
            }

            if (!TryGetAlwaysEnabledEndLineNumber(snapshot, lineIndex + 1, out int endLineNumber))
            {
                continue;
            }

            lineKinds[lineIndex] = "start";
            lineKinds[endLineNumber] = "end";
            lineIndex = endLineNumber;
        }

        return lineKinds;
    }

    private static bool TryGetNextNonBlankLineNumber(
        ITextSnapshot snapshot,
        int startLineNumber,
        out int lineNumber)
    {
        for (int currentLineNumber = startLineNumber;
             currentLineNumber < snapshot.LineCount;
             currentLineNumber++)
        {
            if (!string.IsNullOrWhiteSpace(snapshot.GetLineFromLineNumber(currentLineNumber).GetText()))
            {
                lineNumber = currentLineNumber;
                return true;
            }
        }

        lineNumber = -1;
        return false;
    }

    private static bool TryGetIncludeExcludeEndLineNumber(
        ITextSnapshot snapshot,
        int startLineNumber,
        out int lineNumber)
    {
        for (int currentLineNumber = startLineNumber;
             currentLineNumber < snapshot.LineCount;
             currentLineNumber++)
        {
            if (IsValidIncludeExcludeEndLine(snapshot.GetLineFromLineNumber(currentLineNumber).GetText()))
            {
                lineNumber = currentLineNumber;
                return true;
            }
        }

        lineNumber = -1;
        return false;
    }

    private static bool TryGetAlwaysEnabledEndLineNumber(
        ITextSnapshot snapshot,
        int startLineNumber,
        out int lineNumber)
    {
        for (int currentLineNumber = startLineNumber;
             currentLineNumber < snapshot.LineCount;
             currentLineNumber++)
        {
            if (IsValidAlwaysEnabledBoundaryLine(snapshot.GetLineFromLineNumber(currentLineNumber).GetText(), "end"))
            {
                lineNumber = currentLineNumber;
                return true;
            }
        }

        lineNumber = -1;
        return false;
    }

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
            || !directiveToken.Equals("END", StringComparison.Ordinal))
        {
            return false;
        }

        int pairStart = SkipWhitespace(lineText, directiveTokenEnd);
        if (lineText.IndexOf("include/exclude", pairStart, StringComparison.Ordinal) != pairStart)
        {
            return false;
        }

        for (int i = pairStart + "include/exclude".Length; i < lineText.Length; i++)
        {
            if (!char.IsWhiteSpace(lineText[i]))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsValidAlwaysEnabledBoundaryLine(
        string lineText,
        string expectedBoundary)
    {
        if (!TryGetBiakDirectiveToken(lineText, 0, out _, out string directiveToken, out int directiveTokenEnd)
            || !directiveToken.Equals("always-enabled", StringComparison.Ordinal))
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

    private static string? TryGetDefinedBiakVariableName(string lineText)
    {
        int searchStart = 0;

        while (searchStart < lineText.Length)
        {
            if (!TryGetBiakDirectiveToken(lineText, searchStart, out int markerStart, out string directiveToken, out int directiveTokenEnd))
            {
                return null;
            }

            if (IsBiakVarToken(directiveToken))
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

            searchStart = markerStart + BIAK_MARKER_TOKEN.Length;
        }

        return null;
    }

    private static bool TryGetBiakImportStringBounds(
        string lineText,
        out int stringStart,
        out int stringLength,
        out int commentStart)
    {
        stringStart = 0;
        stringLength = 0;
        commentStart = -1;

        if (!TryGetBiakDirectiveToken(lineText, 0, out _, out string directiveToken, out int directiveTokenEnd)
            || !IsBiakImportToken(directiveToken))
        {
            return false;
        }

        int valueStart = SkipWhitespace(lineText, directiveTokenEnd);
        if (valueStart >= lineText.Length || lineText[valueStart] != '"')
        {
            return false;
        }

        for (int closingQuoteIndex = valueStart + 1; closingQuoteIndex < lineText.Length; closingQuoteIndex++)
        {
            if (lineText[closingQuoteIndex] == '"' && !IsEscaped(lineText, closingQuoteIndex))
            {
                stringStart = valueStart;
                stringLength = closingQuoteIndex - valueStart + 1;
                commentStart = FindInlineCommentStart(lineText, closingQuoteIndex + 1);
                return true;
            }
        }

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
            BIAK_MARKER_TOKEN,
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
            markerStart + BIAK_MARKER_TOKEN.Length);
        return TryReadToken(
            lineText,
            directiveTokenStart,
            out directiveToken,
            out directiveTokenEnd);
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

    private static bool IsBiakVarToken(string directiveToken)
    {
        return directiveToken.Equals("var", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsBiakImportToken(string directiveToken)
    {
        return directiveToken.Equals("import", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsBiakAlwaysEnabledToken(string directiveToken)
    {
        return directiveToken.Equals("always-enabled", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsBiakIncludeToken(string directiveToken)
    {
        return directiveToken.Equals("include", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsBiakExcludeToken(string directiveToken)
    {
        return directiveToken.Equals("exclude", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsBiakStructuralToken(string directiveToken)
    {
        return directiveToken.Equals("END", StringComparison.OrdinalIgnoreCase);
    }

    private static bool LooksLikeBiakVarContinuation(string trimmedStart)
    {
        return trimmedStart.StartsWith("+", StringComparison.Ordinal)
            || trimmedStart.StartsWith("\"", StringComparison.Ordinal)
            || trimmedStart.StartsWith(";", StringComparison.Ordinal);
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

    private static bool IsBiakBaselineToken(string directiveToken)
    {
        return directiveToken.Equals("*-baseline", StringComparison.OrdinalIgnoreCase)
            || directiveToken.EndsWith("-baseline", StringComparison.OrdinalIgnoreCase);
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

        const string INCLUDE_EXCLUDE_PAIR_TOKEN = "include/exclude";
        if (lineText.IndexOf(INCLUDE_EXCLUDE_PAIR_TOKEN, tokenStart, StringComparison.OrdinalIgnoreCase) != tokenStart)
        {
            return;
        }

        spans.Add(new BiakClassifiedSpan(
            tokenStart,
            "include".Length,
            _biakIncludeType));

        spans.Add(new BiakClassifiedSpan(
            tokenStart + "include/".Length,
            "exclude".Length,
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
        out bool isTerminated,
        out bool hasExpressionTokens)
    {
        List<BiakClassifiedSpan> spans = [];
        bool insideString = false;
        int stringStart = -1;
        hasExpressionTokens = false;
        isTerminated = false;

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
                isTerminated = true;
            }
        }

        return spans;
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

    private IEnumerable<TagSpan<ClassificationTag>> CreateCommentTagSpansExcludingBiak(
        ITextSnapshotLine line,
        int commentStart,
        int commentLength,
        IReadOnlyList<BiakClassifiedSpan> biakSpans)
    {
        return CreateTagSpansExcludingBiak(
            line,
            commentStart,
            commentLength,
            _commentType,
            biakSpans);
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
