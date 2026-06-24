// Copyright (c) 2026 kurnakovv
// This file is licensed under the MIT License.
// See the LICENSE file in the project root for full license information.

using System.Drawing;
using System.Drawing.Drawing2D;
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
            BackColor = Color.FromArgb(52, 52, 56),
            ForeColor = Color.White,
            Size = new Size(360, 90),
        };
        toast.Region = CreateRoundedRegion(toast.ClientRectangle, 5);
        toast.Paint += (_, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using Pen borderPen = new(Color.FromArgb(92, 92, 98), 2.25f);
            using GraphicsPath borderPath = CreateRoundedPath(new Rectangle(0, 0, toast.Width - 1, toast.Height - 1), 5);
            e.Graphics.DrawPath(borderPen, borderPath);
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

    private static Region CreateRoundedRegion(Rectangle bounds, int radius)
    {
        using GraphicsPath path = CreateRoundedPath(bounds, radius);
        return new Region(path);
    }

    private static GraphicsPath CreateRoundedPath(Rectangle bounds, int radius)
    {
        GraphicsPath path = new();

        int diameter = radius * 2;
#pragma warning disable KUK0001 // Duplicate arguments passed to method
        path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Y, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.X, bounds.Bottom - diameter, diameter, diameter, 90, 90);
#pragma warning restore KUK0001 // Duplicate arguments passed to method
        path.CloseFigure();

        return path;
    }
}
