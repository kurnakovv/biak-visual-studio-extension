// Copyright (c) 2026 kurnakovv
// This file is licensed under the MIT License.
// See the LICENSE file in the project root for full license information.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Utilities;

namespace BiakVisualStudioExtension;

internal static class EditorConfigVariantContentTypeDefinition
{
    public const string CONTENT_TYPE_NAME = "biak.editorconfig-variant";

#pragma warning disable IDE0051 // Used by MEF composition
#pragma warning disable IDE0044 // MEF export field pattern
    [Export]
    [Name(CONTENT_TYPE_NAME)]
    [BaseDefinition("text")]
    private static ContentTypeDefinition? s_contentTypeDefinition;
#pragma warning restore IDE0044
#pragma warning restore IDE0051
}
