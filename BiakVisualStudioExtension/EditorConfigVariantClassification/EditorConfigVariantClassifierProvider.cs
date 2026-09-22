// Copyright (c) 2026 kurnakovv
// This file is licensed under the MIT License.
// See the LICENSE file in the project root for full license information.

using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace BiakVisualStudioExtension.EditorConfigVariantClassification;

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
