// Copyright (c) 2026 kurnakovv
// This file is licensed under the MIT License.
// See the LICENSE file in the project root for full license information.

namespace BiakVisualStudioExtension.Constants;

internal static class BiakDirectiveTokenConstant
{
    public const string BIAK_MARKER_TOKEN = "^biak^";
    public const string VAR = "var";
    public const string IMPORT = "import";
    public const string ALWAYS_ENABLED = "always-enabled";
    public const string INCLUDE = "include";
    public const string EXCLUDE = "exclude";
    public const string INCLUDE_EXCLUDE_PAIR = INCLUDE + "/" + EXCLUDE;
}
