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
    public const string BIAK_MARKER_CLASSIFICATION_TYPE_NAME = "biak.editorconfig-variant.biak-marker";
    public const string BIAK_VAR_CLASSIFICATION_TYPE_NAME = "biak.editorconfig-variant.biak-var";
    public const string BIAK_IMPORT_CLASSIFICATION_TYPE_NAME = "biak.editorconfig-variant.biak-import";
    public const string BIAK_ALWAYS_ENABLED_CLASSIFICATION_TYPE_NAME = "biak.editorconfig-variant.biak-always-enabled";
    public const string BIAK_INCLUDE_CLASSIFICATION_TYPE_NAME = "biak.editorconfig-variant.biak-include";
    public const string BIAK_EXCLUDE_CLASSIFICATION_TYPE_NAME = "biak.editorconfig-variant.biak-exclude";
    public const string BIAK_STRUCTURAL_CLASSIFICATION_TYPE_NAME = "biak.editorconfig-variant.biak-structural";
    public const string BIAK_VARIABLE_NAME_CLASSIFICATION_TYPE_NAME = "biak.editorconfig-variant.biak-variable-name";
    public const string BIAK_BASELINE_CLASSIFICATION_TYPE_NAME = "biak.editorconfig-variant.biak-baseline";
    public const string KEY_CLASSIFICATION_TYPE_NAME = "biak.editorconfig-variant.key";
    public const string ERROR_CLASSIFICATION_TYPE_NAME = "biak.editorconfig-variant.severity.error";
    public const string WARNING_CLASSIFICATION_TYPE_NAME = "biak.editorconfig-variant.severity.warning";
    public const string SUGGESTION_CLASSIFICATION_TYPE_NAME = "biak.editorconfig-variant.severity.suggestion";
    public const string NONE_CLASSIFICATION_TYPE_NAME = "biak.editorconfig-variant.severity.none";
    public const string SILENT_CLASSIFICATION_TYPE_NAME = "biak.editorconfig-variant.severity.silent";

    // ReSharper disable UnassignedField.Global, UnassignedField.Local, UnassignedField.Compiler
#pragma warning disable CS0169, IDE0051, IDE0044 // Used by MEF composition + MEF export field pattern
    [Export(typeof(ClassificationTypeDefinition))]
    [Name(BIAK_MARKER_CLASSIFICATION_TYPE_NAME)]
    [BaseDefinition("text")]
    private static ClassificationTypeDefinition? s_biakMarkerClassificationTypeDefinition;

    [Export(typeof(ClassificationTypeDefinition))]
    [Name(BIAK_VAR_CLASSIFICATION_TYPE_NAME)]
    [BaseDefinition("text")]
    private static ClassificationTypeDefinition? s_biakVarClassificationTypeDefinition;

    [Export(typeof(ClassificationTypeDefinition))]
    [Name(BIAK_IMPORT_CLASSIFICATION_TYPE_NAME)]
    [BaseDefinition("text")]
    private static ClassificationTypeDefinition? s_biakImportClassificationTypeDefinition;

    [Export(typeof(ClassificationTypeDefinition))]
    [Name(BIAK_ALWAYS_ENABLED_CLASSIFICATION_TYPE_NAME)]
    [BaseDefinition("text")]
    private static ClassificationTypeDefinition? s_biakAlwaysEnabledClassificationTypeDefinition;

    [Export(typeof(ClassificationTypeDefinition))]
    [Name(BIAK_INCLUDE_CLASSIFICATION_TYPE_NAME)]
    [BaseDefinition("text")]
    private static ClassificationTypeDefinition? s_biakIncludeClassificationTypeDefinition;

    [Export(typeof(ClassificationTypeDefinition))]
    [Name(BIAK_EXCLUDE_CLASSIFICATION_TYPE_NAME)]
    [BaseDefinition("text")]
    private static ClassificationTypeDefinition? s_biakExcludeClassificationTypeDefinition;

    [Export(typeof(ClassificationTypeDefinition))]
    [Name(BIAK_STRUCTURAL_CLASSIFICATION_TYPE_NAME)]
    [BaseDefinition("text")]
    private static ClassificationTypeDefinition? s_biakStructuralClassificationTypeDefinition;

    [Export(typeof(ClassificationTypeDefinition))]
    [Name(BIAK_VARIABLE_NAME_CLASSIFICATION_TYPE_NAME)]
    [BaseDefinition("text")]
    private static ClassificationTypeDefinition? s_biakVariableNameClassificationTypeDefinition;

    [Export(typeof(ClassificationTypeDefinition))]
    [Name(BIAK_BASELINE_CLASSIFICATION_TYPE_NAME)]
    [BaseDefinition("text")]
    private static ClassificationTypeDefinition? s_biakBaselineClassificationTypeDefinition;

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
#pragma warning restore CS0169, IDE0051, IDE0044
    // ReSharper restore UnassignedField.Global, UnassignedField.Local, UnassignedField.Compiler
}

[Export(typeof(EditorFormatDefinition))]
[ClassificationType(ClassificationTypeNames = EditorConfigVariantSeverityClassificationDefinitions.BIAK_MARKER_CLASSIFICATION_TYPE_NAME)]
[Name(EditorConfigVariantSeverityClassificationDefinitions.BIAK_MARKER_CLASSIFICATION_TYPE_NAME)]
[UserVisible(true)]
[Order(Before = Priority.Default)]
internal sealed class EditorConfigVariantBiakMarkerFormatDefinition : ClassificationFormatDefinition
{
    public EditorConfigVariantBiakMarkerFormatDefinition()
    {
        DisplayName = "biak marker (^biak^)";
        ForegroundColor = Colors.MediumOrchid;
    }
}

[Export(typeof(EditorFormatDefinition))]
[ClassificationType(ClassificationTypeNames = EditorConfigVariantSeverityClassificationDefinitions.BIAK_VAR_CLASSIFICATION_TYPE_NAME)]
[Name(EditorConfigVariantSeverityClassificationDefinitions.BIAK_VAR_CLASSIFICATION_TYPE_NAME)]
[UserVisible(true)]
[Order(Before = Priority.Default)]
internal sealed class EditorConfigVariantBiakVarFormatDefinition : ClassificationFormatDefinition
{
    public EditorConfigVariantBiakVarFormatDefinition()
    {
        DisplayName = "biak directive: var";
        ForegroundColor = Color.FromRgb(86, 156, 214);
    }
}

[Export(typeof(EditorFormatDefinition))]
[ClassificationType(ClassificationTypeNames = EditorConfigVariantSeverityClassificationDefinitions.BIAK_IMPORT_CLASSIFICATION_TYPE_NAME)]
[Name(EditorConfigVariantSeverityClassificationDefinitions.BIAK_IMPORT_CLASSIFICATION_TYPE_NAME)]
[UserVisible(true)]
[Order(Before = Priority.Default)]
internal sealed class EditorConfigVariantBiakImportFormatDefinition : ClassificationFormatDefinition
{
    public EditorConfigVariantBiakImportFormatDefinition()
    {
        DisplayName = "biak directive: import";
        ForegroundColor = Color.FromRgb(197, 134, 192);
    }
}

[Export(typeof(EditorFormatDefinition))]
[ClassificationType(ClassificationTypeNames = EditorConfigVariantSeverityClassificationDefinitions.BIAK_ALWAYS_ENABLED_CLASSIFICATION_TYPE_NAME)]
[Name(EditorConfigVariantSeverityClassificationDefinitions.BIAK_ALWAYS_ENABLED_CLASSIFICATION_TYPE_NAME)]
[UserVisible(true)]
[Order(Before = Priority.Default)]
internal sealed class EditorConfigVariantBiakAlwaysEnabledFormatDefinition : ClassificationFormatDefinition
{
    public EditorConfigVariantBiakAlwaysEnabledFormatDefinition()
    {
        DisplayName = "biak directive: always-enabled";
        ForegroundColor = Color.FromRgb(220, 140, 60);
    }
}

[Export(typeof(EditorFormatDefinition))]
[ClassificationType(ClassificationTypeNames = EditorConfigVariantSeverityClassificationDefinitions.BIAK_INCLUDE_CLASSIFICATION_TYPE_NAME)]
[Name(EditorConfigVariantSeverityClassificationDefinitions.BIAK_INCLUDE_CLASSIFICATION_TYPE_NAME)]
[UserVisible(true)]
[Order(Before = Priority.Default)]
internal sealed class EditorConfigVariantBiakIncludeFormatDefinition : ClassificationFormatDefinition
{
    public EditorConfigVariantBiakIncludeFormatDefinition()
    {
        DisplayName = "biak directive: include";
        ForegroundColor = Color.FromRgb(120, 170, 95);
    }
}

[Export(typeof(EditorFormatDefinition))]
[ClassificationType(ClassificationTypeNames = EditorConfigVariantSeverityClassificationDefinitions.BIAK_EXCLUDE_CLASSIFICATION_TYPE_NAME)]
[Name(EditorConfigVariantSeverityClassificationDefinitions.BIAK_EXCLUDE_CLASSIFICATION_TYPE_NAME)]
[UserVisible(true)]
[Order(Before = Priority.Default)]
internal sealed class EditorConfigVariantBiakExcludeFormatDefinition : ClassificationFormatDefinition
{
    public EditorConfigVariantBiakExcludeFormatDefinition()
    {
        DisplayName = "biak directive: exclude";
        ForegroundColor = Color.FromRgb(244, 71, 71);
    }
}

[Export(typeof(EditorFormatDefinition))]
[ClassificationType(ClassificationTypeNames = EditorConfigVariantSeverityClassificationDefinitions.BIAK_STRUCTURAL_CLASSIFICATION_TYPE_NAME)]
[Name(EditorConfigVariantSeverityClassificationDefinitions.BIAK_STRUCTURAL_CLASSIFICATION_TYPE_NAME)]
[UserVisible(true)]
[Order(Before = Priority.Default)]
internal sealed class EditorConfigVariantBiakStructuralFormatDefinition : ClassificationFormatDefinition
{
    public EditorConfigVariantBiakStructuralFormatDefinition()
    {
        DisplayName = "biak structural keyword";
        ForegroundColor = Color.FromRgb(128, 128, 128);
    }
}

[Export(typeof(EditorFormatDefinition))]
[ClassificationType(ClassificationTypeNames = EditorConfigVariantSeverityClassificationDefinitions.BIAK_VARIABLE_NAME_CLASSIFICATION_TYPE_NAME)]
[Name(EditorConfigVariantSeverityClassificationDefinitions.BIAK_VARIABLE_NAME_CLASSIFICATION_TYPE_NAME)]
[UserVisible(true)]
[Order(Before = Priority.Default)]
internal sealed class EditorConfigVariantBiakVariableNameFormatDefinition : ClassificationFormatDefinition
{
    public EditorConfigVariantBiakVariableNameFormatDefinition()
    {
        DisplayName = "biak variable name";
        ForegroundColor = Color.FromRgb(156, 220, 254);
    }
}

[Export(typeof(EditorFormatDefinition))]
[ClassificationType(ClassificationTypeNames = EditorConfigVariantSeverityClassificationDefinitions.BIAK_BASELINE_CLASSIFICATION_TYPE_NAME)]
[Name(EditorConfigVariantSeverityClassificationDefinitions.BIAK_BASELINE_CLASSIFICATION_TYPE_NAME)]
[UserVisible(true)]
[Order(Before = Priority.Default)]
internal sealed class EditorConfigVariantBiakBaselineFormatDefinition : ClassificationFormatDefinition
{
    public EditorConfigVariantBiakBaselineFormatDefinition()
    {
        DisplayName = "biak directive: *-baseline";
        ForegroundColor = Colors.Gold;
    }
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
