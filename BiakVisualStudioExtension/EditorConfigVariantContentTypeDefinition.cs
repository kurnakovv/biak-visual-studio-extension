// Copyright (c) 2026 kurnakovv
// This file is licensed under the MIT License.
// See the LICENSE file in the project root for full license information.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Utilities;

namespace BiakVisualStudioExtension;

internal static class EditorConfigVariantContentTypeDefinition
{
    public const string CONTENT_TYPE_NAME = "biak.editorconfig-variant";

#pragma warning disable CS0169,IDE0051,IDE0044 // Used by MEF composition + MEF export field pattern
    [Export]
    [Name(CONTENT_TYPE_NAME)]
    [BaseDefinition("text")]
    private static ContentTypeDefinition? s_contentTypeDefinition;
#pragma warning restore CS0169,IDE0051,IDE0044
}
