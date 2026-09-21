// Copyright (c) 2026 kurnakovv
// This file is licensed under the MIT License.
// See the LICENSE file in the project root for full license information.

using System.Collections.Generic;
using BiakVisualStudioExtension.Constants;

namespace BiakVisualStudioExtension.EditorConfigVariantClassification;

internal sealed partial class EditorConfigVariantClassifier
{
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
            || directiveToken != BiakDirectiveTokenConstant.IMPORT)
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

    private bool TryAddImportDirectiveSpans(
        string directiveToken,
        int markerStart,
        int tokenStart,
        int tokenLength,
        ICollection<BiakClassifiedSpan> spans)
    {
        if (directiveToken != BiakDirectiveTokenConstant.IMPORT)
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
            _biakImportType));

        return true;
    }
}
