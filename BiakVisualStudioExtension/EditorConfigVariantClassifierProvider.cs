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

        return new EditorConfigVariantClassifier(
            ClassificationTypeRegistryService) as ITagger<T>;
    }
}

internal sealed class EditorConfigVariantClassifier : ITagger<ClassificationTag>
{
    private readonly IClassificationType _commentType;
    private readonly IClassificationType _keywordType;
    private readonly IClassificationType _identifierType;
    private readonly IClassificationType _keyType;
    private readonly IClassificationType _operatorType;
    private readonly IClassificationType _stringType;
    private readonly IClassificationType _severityErrorType;
    private readonly IClassificationType _severityWarningType;
    private readonly IClassificationType _severitySuggestionType;
    private readonly IClassificationType _severityNoneType;
    private readonly IClassificationType _severitySilentType;

    public EditorConfigVariantClassifier(
        IClassificationTypeRegistryService classificationTypeRegistryService)
    {
        if (classificationTypeRegistryService is null)
        {
            throw new ArgumentNullException(
                nameof(classificationTypeRegistryService));
        }

        _commentType = classificationTypeRegistryService.GetClassificationType(
            PredefinedClassificationTypeNames.Comment);
        _keywordType = classificationTypeRegistryService.GetClassificationType(
            PredefinedClassificationTypeNames.Keyword);
        _identifierType = classificationTypeRegistryService.GetClassificationType(
            PredefinedClassificationTypeNames.Identifier);
        _keyType = classificationTypeRegistryService.GetClassificationType(
            EditorConfigVariantSeverityClassificationDefinitions.KEY_CLASSIFICATION_TYPE_NAME)
            ?? _identifierType;
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

        foreach (SnapshotSpan span in spans)
        {
            ITextSnapshotLine line = span.Start.GetContainingLine();
            string lineText = line.GetText();

            if (string.IsNullOrWhiteSpace(lineText))
            {
                continue;
            }

            string trimmedStart = lineText.TrimStart();
            int indent = lineText.Length - trimmedStart.Length;

            if (trimmedStart.StartsWith("#", StringComparison.Ordinal)
                || trimmedStart.StartsWith(";", StringComparison.Ordinal))
            {
                yield return CreateTagSpan(
                    line,
                    indent,
                    trimmedStart.Length,
                    _commentType);
                continue;
            }

            if (trimmedStart.StartsWith("[", StringComparison.Ordinal)
                && trimmedStart.Contains("]", StringComparison.Ordinal))
            {
                yield return CreateTagSpan(
                    line,
                    indent,
                    trimmedStart.Length,
                    _keywordType);
                continue;
            }

            int equalsIndex = lineText.IndexOf('=');
            if (equalsIndex < 0)
            {
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
                yield return CreateTagSpan(
                    line,
                    keyStart,
                    keyEnd - keyStart + 1,
                    _keyType);
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

            string keyText = lineText.Substring(
                keyStart,
                keyEnd - keyStart + 1);

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
                else
                {
                    yield return CreateTagSpan(
                        line,
                        valueStart,
                        valueEndExclusive - valueStart,
                        _stringType);
                }
            }

            if (inlineCommentStart >= 0)
            {
                yield return CreateTagSpan(
                    line,
                    inlineCommentStart,
                    lineText.Length - inlineCommentStart,
                    _commentType);
            }
        }
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
        return keyText.EndsWith(
            ".severity",
            StringComparison.OrdinalIgnoreCase);
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
}
