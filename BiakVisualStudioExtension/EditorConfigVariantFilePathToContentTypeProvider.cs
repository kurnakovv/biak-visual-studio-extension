// Copyright (c) 2026 kurnakovv
// This file is licensed under the MIT License.
// See the LICENSE file in the project root for full license information.

using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using Microsoft.VisualStudio.Utilities;

namespace BiakVisualStudioExtension;

[Export(typeof(IFilePathToContentTypeProvider))]
[Name(nameof(EditorConfigVariantFilePathToContentTypeProvider))]
[FileExtension("*")]
internal sealed class EditorConfigVariantFilePathToContentTypeProvider
    : IFilePathToContentTypeProvider
{
    private readonly IContentTypeRegistryService _contentTypeRegistryService;

    [ImportingConstructor]
    public EditorConfigVariantFilePathToContentTypeProvider(
        IContentTypeRegistryService contentTypeRegistryService)
    {
        _contentTypeRegistryService =
            contentTypeRegistryService
            ?? throw new ArgumentNullException(nameof(contentTypeRegistryService));

        Debug.WriteLine("!!! PROVIDER CREATED");
    }

    public bool TryGetContentTypeForFilePath(
        string filePath,
        out IContentType contentType)
    {
        Debug.WriteLine($"!!! PROVIDER CALLED: {filePath}");

        contentType = null!;

        string fileName = Path.GetFileName(filePath);

        if (!fileName.StartsWith(
            ".editorconfig-",
            StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        contentType =
            _contentTypeRegistryService.GetContentType(
                EditorConfigVariantContentTypeDefinition.CONTENT_TYPE_NAME)!;

        Debug.WriteLine(
            $"!!! CONTENT TYPE: {contentType?.TypeName ?? "<null>"}");

        return contentType is not null;
    }
}
