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

        Button closeButton = new()
        {
            Text = "X",
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            FlatStyle = FlatStyle.Flat,
            ForeColor = Color.White,
            BackColor = Color.FromArgb(62, 62, 64),
            UseVisualStyleBackColor = false,
            TabStop = false,
            Cursor = Cursors.Hand,
            Location = new Point(330, 8),
            Size = new Size(20, 20),
        };
        closeButton.FlatAppearance.BorderSize = 0;
        closeButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(80, 80, 82);
        closeButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(80, 80, 82);

        toast.Controls.Add(titleLabel);
        toast.Controls.Add(messageLabel);
        toast.Controls.Add(closeButton);
        closeButton.BringToFront();

        Rectangle workArea = Screen.FromPoint(Cursor.Position).WorkingArea;
        toast.Location = new Point(workArea.Right - toast.Width - 16, workArea.Bottom - toast.Height - 16);

        Timer timer = new() { Interval = 4000 };
        bool isClosing = false;

        void CloseToast()
        {
            if (isClosing || toast.IsDisposed)
            {
                return;
            }

            isClosing = true;
            timer.Stop();
            timer.Dispose();
            toast.Close();
        }

        timer.Tick += (_, _) => CloseToast();
        closeButton.Click += (_, _) => CloseToast();
        toast.Shown += (_, _) => timer.Start();
        toast.Show();
    }
}
