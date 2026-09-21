// Copyright (c) 2026 kurnakovv
// This file is licensed under the MIT License.
// See the LICENSE file in the project root for full license information.

using System.Collections.Generic;
using BiakVisualStudioExtension.Constants;

namespace BiakVisualStudioExtension.EditorConfigVariantClassification;

internal sealed partial class EditorConfigVariantClassifier
{
    private bool TryAddBaselineDirectiveSpans(
        string directiveToken,
        int markerStart,
        int tokenStart,
        int tokenLength,
        ICollection<BiakClassifiedSpan> spans)
    {
        if (directiveToken is not ("inspectcode-baseline" or "warnings-baseline"))
        {
            return false;
        }

        spans.Add(new BiakClassifiedSpan(
            markerStart,
            BiakDirectiveTokenConstant.BIAK_MARKER_TOKEN.Length,
            _biakMarkerType));

        spans.Add(new BiakClassifiedSpan(
            tokenStart,
            tokenLength,
            _biakBaselineType));

        return true;
    }
}
