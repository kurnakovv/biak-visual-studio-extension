// Copyright (c) 2026 kurnakovv
// This file is licensed under the MIT License.
// See the LICENSE file in the project root for full license information.

using System.ComponentModel.Composition;
using System.Windows.Media;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace BiakVisualStudioExtension;

internal static class EditorConfigVariantSeverityClassificationDefinitions
{
    public const string KEY_CLASSIFICATION_TYPE_NAME = "biak.editorconfig-variant.key";
    public const string ERROR_CLASSIFICATION_TYPE_NAME = "biak.editorconfig-variant.severity.error";
    public const string WARNING_CLASSIFICATION_TYPE_NAME = "biak.editorconfig-variant.severity.warning";
    public const string SUGGESTION_CLASSIFICATION_TYPE_NAME = "biak.editorconfig-variant.severity.suggestion";
    public const string NONE_CLASSIFICATION_TYPE_NAME = "biak.editorconfig-variant.severity.none";
    public const string SILENT_CLASSIFICATION_TYPE_NAME = "biak.editorconfig-variant.severity.silent";

    // ReSharper disable UnassignedField.Global
#pragma warning disable CS0169,IDE0051,IDE0044 // Used by MEF composition + MEF export field pattern
    [Export(typeof(ClassificationTypeDefinition))]
    [Name(KEY_CLASSIFICATION_TYPE_NAME)]
    [BaseDefinition("text")]
    private static ClassificationTypeDefinition? s_keyClassificationTypeDefinition;

    [Export(typeof(ClassificationTypeDefinition))]
    [Name(ERROR_CLASSIFICATION_TYPE_NAME)]
    [BaseDefinition("text")]
    private static ClassificationTypeDefinition? s_errorClassificationTypeDefinition;

    [Export(typeof(ClassificationTypeDefinition))]
    [Name(WARNING_CLASSIFICATION_TYPE_NAME)]
    [BaseDefinition("text")]
    private static ClassificationTypeDefinition? s_warningClassificationTypeDefinition;

    [Export(typeof(ClassificationTypeDefinition))]
    [Name(SUGGESTION_CLASSIFICATION_TYPE_NAME)]
    [BaseDefinition("text")]
    private static ClassificationTypeDefinition? s_suggestionClassificationTypeDefinition;

    [Export(typeof(ClassificationTypeDefinition))]
    [Name(NONE_CLASSIFICATION_TYPE_NAME)]
    [BaseDefinition("text")]
    private static ClassificationTypeDefinition? s_noneClassificationTypeDefinition;

    [Export(typeof(ClassificationTypeDefinition))]
    [Name(SILENT_CLASSIFICATION_TYPE_NAME)]
    [BaseDefinition("text")]
    private static ClassificationTypeDefinition? s_silentClassificationTypeDefinition;
#pragma warning restore CS0169,IDE0051,IDE0044
    // ReSharper restore UnassignedField.Global
}

[Export(typeof(EditorFormatDefinition))]
[ClassificationType(ClassificationTypeNames = EditorConfigVariantSeverityClassificationDefinitions.KEY_CLASSIFICATION_TYPE_NAME)]
[Name(EditorConfigVariantSeverityClassificationDefinitions.KEY_CLASSIFICATION_TYPE_NAME)]
[UserVisible(true)]
[Order(Before = Priority.Default)]
internal sealed class EditorConfigVariantKeyFormatDefinition : ClassificationFormatDefinition
{
    public EditorConfigVariantKeyFormatDefinition()
    {
        DisplayName = "biak editorconfig key";
        ForegroundColor = Colors.Turquoise;
    }
}

[Export(typeof(EditorFormatDefinition))]
[ClassificationType(ClassificationTypeNames = EditorConfigVariantSeverityClassificationDefinitions.ERROR_CLASSIFICATION_TYPE_NAME)]
[Name(EditorConfigVariantSeverityClassificationDefinitions.ERROR_CLASSIFICATION_TYPE_NAME)]
[UserVisible(true)]
[Order(Before = Priority.Default)]
internal sealed class EditorConfigVariantSeverityErrorFormatDefinition : ClassificationFormatDefinition
{
    public EditorConfigVariantSeverityErrorFormatDefinition()
    {
        DisplayName = "biak editorconfig severity: error";
        ForegroundColor = Colors.IndianRed;
    }
}

[Export(typeof(EditorFormatDefinition))]
[ClassificationType(ClassificationTypeNames = EditorConfigVariantSeverityClassificationDefinitions.WARNING_CLASSIFICATION_TYPE_NAME)]
[Name(EditorConfigVariantSeverityClassificationDefinitions.WARNING_CLASSIFICATION_TYPE_NAME)]
[UserVisible(true)]
[Order(Before = Priority.Default)]
internal sealed class EditorConfigVariantSeverityWarningFormatDefinition : ClassificationFormatDefinition
{
    public EditorConfigVariantSeverityWarningFormatDefinition()
    {
        DisplayName = "biak editorconfig severity: warning";
        ForegroundColor = Colors.LightGoldenrodYellow;
    }
}

[Export(typeof(EditorFormatDefinition))]
[ClassificationType(ClassificationTypeNames = EditorConfigVariantSeverityClassificationDefinitions.SUGGESTION_CLASSIFICATION_TYPE_NAME)]
[Name(EditorConfigVariantSeverityClassificationDefinitions.SUGGESTION_CLASSIFICATION_TYPE_NAME)]
[UserVisible(true)]
[Order(Before = Priority.Default)]
internal sealed class EditorConfigVariantSeveritySuggestionFormatDefinition : ClassificationFormatDefinition
{
    public EditorConfigVariantSeveritySuggestionFormatDefinition()
    {
        DisplayName = "biak editorconfig severity: suggestion";
        ForegroundColor = Colors.LightBlue;
    }
}

[Export(typeof(EditorFormatDefinition))]
[ClassificationType(ClassificationTypeNames = EditorConfigVariantSeverityClassificationDefinitions.NONE_CLASSIFICATION_TYPE_NAME)]
[Name(EditorConfigVariantSeverityClassificationDefinitions.NONE_CLASSIFICATION_TYPE_NAME)]
[UserVisible(true)]
[Order(Before = Priority.Default)]
internal sealed class EditorConfigVariantSeverityNoneFormatDefinition : ClassificationFormatDefinition
{
    public EditorConfigVariantSeverityNoneFormatDefinition()
    {
        DisplayName = "biak editorconfig severity: none";
        ForegroundColor = Colors.Gray;
    }
}

[Export(typeof(EditorFormatDefinition))]
[ClassificationType(ClassificationTypeNames = EditorConfigVariantSeverityClassificationDefinitions.SILENT_CLASSIFICATION_TYPE_NAME)]
[Name(EditorConfigVariantSeverityClassificationDefinitions.SILENT_CLASSIFICATION_TYPE_NAME)]
[UserVisible(true)]
[Order(Before = Priority.Default)]
internal sealed class EditorConfigVariantSeveritySilentFormatDefinition : ClassificationFormatDefinition
{
    public EditorConfigVariantSeveritySilentFormatDefinition()
    {
        DisplayName = "biak editorconfig severity: silent/hidden";
        ForegroundColor = Colors.SlateGray;
    }
}
