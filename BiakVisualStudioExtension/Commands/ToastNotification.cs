// Copyright (c) 2026 kurnakovv
// This file is licensed under the MIT License.
// See the LICENSE file in the project root for full license information.

using System.Drawing;
using System.Windows.Forms;
using Microsoft.VisualStudio.Shell;

namespace BiakVisualStudioExtension.Commands;

internal static class ToastNotification
{
    public static void Show(string title, string message)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        Form toast = new()
        {
            FormBorderStyle = FormBorderStyle.None,
            StartPosition = FormStartPosition.Manual,
            ShowInTaskbar = false,
            TopMost = true,
            BackColor = Color.FromArgb(37, 37, 38),
            ForeColor = Color.White,
            Size = new Size(360, 90),
        };

        Label titleLabel = new()
        {
            Text = title,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            ForeColor = Color.White,
            Location = new Point(12, 10),
            Size = new Size(336, 22),
        };

        Label messageLabel = new()
        {
            Text = message,
            Font = new Font("Segoe UI", 9),
            ForeColor = Color.Gainsboro,
            Location = new Point(12, 34),
            Size = new Size(336, 42),
        };

        toast.Controls.Add(titleLabel);
        toast.Controls.Add(messageLabel);

        Rectangle workArea = Screen.FromPoint(Cursor.Position).WorkingArea;
        toast.Location = new Point(workArea.Right - toast.Width - 16, workArea.Bottom - toast.Height - 16);

        Timer timer = new() { Interval = 4000 };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            timer.Dispose();
            toast.Close();
            toast.Dispose();
        };

        toast.Shown += (_, _) => timer.Start();
        toast.Show();
    }
}
