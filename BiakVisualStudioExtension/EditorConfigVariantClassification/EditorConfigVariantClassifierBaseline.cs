// Copyright (c) 2026 kurnakovv
// This file is licensed under the MIT License.
// See the LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace BiakVisualStudioExtension.EditorConfigVariantClassification;

internal sealed partial class EditorConfigVariantClassifier
{
    private void TryAddBaselineDirectiveSpans(
        string directiveToken,
        int tokenStart,
        int tokenLength,
        ICollection<BiakClassifiedSpan> spans)
    {
        if (directiveToken is not ("inspectcode-baseline" or "warnings-baseline"))
        {
            return;
        }

        spans.Add(new BiakClassifiedSpan(
            tokenStart,
            tokenLength,
            _biakBaselineType));
    }
}
