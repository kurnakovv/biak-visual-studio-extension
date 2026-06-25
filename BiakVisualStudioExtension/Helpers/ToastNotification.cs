// Copyright (c) 2026 kurnakovv
// This file is licensed under the MIT License.
// See the LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Microsoft.VisualStudio.Shell;

namespace BiakVisualStudioExtension.Helpers;

internal enum ToastIconKind
{
    None = 0,
    Success = 1,
    Error = 2,
    Warning = 3,
    Info = 4,
}

internal static class ToastNotification
{
    private const int TOAST_WIDTH = 360;
    private const int MIN_TOAST_HEIGHT = 90;
    private const int MAX_TOAST_HEIGHT = 260;
    private const int HORIZONTAL_PADDING = 12;
    private const int TOP_PADDING = 10;
    private const int BOTTOM_PADDING = 12;
    private const int TITLE_HEIGHT = 22;
    private const int CONTENT_SPACING = 2;
    private const int CLOSE_BUTTON_SIZE = 20;
    private const int ICON_SIZE = 16;
    private const int ICON_SPACING = 8;

    public static void Show(string title, string message, ToastIconKind iconKind)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        Show(title, message, null, iconKind);
    }

    public static void Show(string title, string message, string? linkUrl, ToastIconKind iconKind)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        bool hasLink = !string.IsNullOrWhiteSpace(linkUrl);
        bool hasIcon = iconKind != ToastIconKind.None;
        int iconOffset = hasIcon ? ICON_SIZE + ICON_SPACING : 0;
        int textStartX = HORIZONTAL_PADDING + iconOffset;
        int contentWidth = TOAST_WIDTH - textStartX - HORIZONTAL_PADDING;
        int titleWidth = contentWidth - CLOSE_BUTTON_SIZE - 8;
        int linkSpacing = hasLink ? 8 : 0;
        int linkHeight = hasLink ? 20 : 0;

        using Font measureFont = new("Segoe UI", 9);
        Size measuredMessageSize = TextRenderer.MeasureText(
            message,
            measureFont,
            new Size(contentWidth, int.MaxValue),
            TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl);

        int availableMessageHeight = MAX_TOAST_HEIGHT - (TOP_PADDING + TITLE_HEIGHT + CONTENT_SPACING + linkSpacing + linkHeight + BOTTOM_PADDING);
        int messageHeight = Math.Max(42, Math.Min(measuredMessageSize.Height, availableMessageHeight));
        int toastHeight = Math.Max(MIN_TOAST_HEIGHT, TOP_PADDING + TITLE_HEIGHT + CONTENT_SPACING + messageHeight + linkSpacing + linkHeight + BOTTOM_PADDING);

        Form toast = new()
        {
            FormBorderStyle = FormBorderStyle.None,
            StartPosition = FormStartPosition.Manual,
            ShowInTaskbar = false,
            TopMost = true,
            BackColor = Color.FromArgb(52, 52, 56),
            ForeColor = Color.White,
            Size = new Size(TOAST_WIDTH, toastHeight),
        };
        toast.Region = CreateRoundedRegion(toast.ClientRectangle, 5);
        toast.Paint += (_, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using Pen borderPen = new(Color.FromArgb(92, 92, 98), 2.25f);
            using GraphicsPath borderPath = CreateRoundedPath(new Rectangle(0, 0, toast.Width - 1, toast.Height - 1), 5);
            e.Graphics.DrawPath(borderPen, borderPath);
        };

        PictureBox? iconPictureBox = null;
        if (hasIcon)
        {
            iconPictureBox = new PictureBox
            {
                Image = GetIconImage(iconKind),
                SizeMode = PictureBoxSizeMode.StretchImage,
                BackColor = Color.Transparent,
                Location = new Point(HORIZONTAL_PADDING, TOP_PADDING + 2),
                Size = new Size(ICON_SIZE, ICON_SIZE),
                TabStop = false,
            };
            toast.Controls.Add(iconPictureBox);
        }

        Label titleLabel = new()
        {
            Text = title,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            ForeColor = Color.White,
            Location = new Point(textStartX, TOP_PADDING),
            Size = new Size(titleWidth, TITLE_HEIGHT),
        };

        Label messageLabel = new()
        {
            Text = message,
            Font = new Font("Segoe UI", 9),
            ForeColor = Color.Gainsboro,
            Location = new Point(textStartX, TOP_PADDING + TITLE_HEIGHT + CONTENT_SPACING),
            Size = new Size(contentWidth, messageHeight),
            AutoEllipsis = measuredMessageSize.Height > messageHeight,
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
            Location = new Point(TOAST_WIDTH - HORIZONTAL_PADDING - CLOSE_BUTTON_SIZE, TOP_PADDING - 2),
            Size = new Size(CLOSE_BUTTON_SIZE, CLOSE_BUTTON_SIZE),
        };
        closeButton.FlatAppearance.BorderSize = 0;
        closeButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(80, 80, 82);
        closeButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(80, 80, 82);

        toast.Controls.Add(titleLabel);
        toast.Controls.Add(messageLabel);

        if (hasLink)
        {
            LinkLabel linkLabel = new()
            {
                Text = linkUrl,
                Font = new Font("Segoe UI", 9, FontStyle.Underline),
                LinkColor = Color.DeepSkyBlue,
                ActiveLinkColor = Color.DodgerBlue,
                VisitedLinkColor = Color.DeepSkyBlue,
                BackColor = Color.Transparent,
                Location = new Point(textStartX, messageLabel.Bottom + linkSpacing),
                Size = new Size(contentWidth, linkHeight),
                TabStop = true,
                Cursor = Cursors.Hand,
            };
            linkLabel.LinkClicked += (_, _) => OpenLink(linkUrl!);
            toast.Controls.Add(linkLabel);
        }

        toast.Controls.Add(closeButton);
        closeButton.BringToFront();

        Rectangle workArea = Screen.FromPoint(Cursor.Position).WorkingArea;
        toast.Location = new Point(workArea.Right - toast.Width - 16, workArea.Bottom - toast.Height - 16);

        Timer timer = new() { Interval = hasLink ? 7000 : 4000 };
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

            if (iconPictureBox?.Image is not null)
            {
                iconPictureBox.Image.Dispose();
                iconPictureBox.Image = null;
            }

            toast.Close();
        }

        timer.Tick += (_, _) => CloseToast();
        closeButton.Click += (_, _) => CloseToast();
        toast.Shown += (_, _) => timer.Start();
        toast.Show();
    }

    private static Image GetIconImage(ToastIconKind iconKind)
    {
        return iconKind switch
        {
            ToastIconKind.None => SystemIcons.Information.ToBitmap(),
            ToastIconKind.Success => CreateSuccessIcon(),
            ToastIconKind.Error => SystemIcons.Error.ToBitmap(),
            ToastIconKind.Warning => SystemIcons.Warning.ToBitmap(),
            ToastIconKind.Info => SystemIcons.Information.ToBitmap(),
            _ => throw new NotImplementedException($"Icon kind {iconKind} is not implemented."),
        };
    }

    private static Bitmap CreateSuccessIcon()
    {
        Bitmap icon = new(ICON_SIZE, ICON_SIZE);

        using Graphics graphics = Graphics.FromImage(icon);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;

        using SolidBrush background = new(Color.FromArgb(38, 172, 94));
        graphics.FillEllipse(background, 0, 0, ICON_SIZE - 1, ICON_SIZE - 1);

        using Pen checkPen = new(Color.White, 2f);
        checkPen.StartCap = LineCap.Round;
        checkPen.EndCap = LineCap.Round;

        graphics.DrawLines(
            checkPen,
            [
                new Point(4, 8),
                new Point(7, 11),
                new Point(12, 5),
            ]);

        return icon;
    }

    private static void OpenLink(string linkUrl)
    {
        if (!Uri.TryCreate(linkUrl, UriKind.Absolute, out Uri? uri))
        {
            return;
        }

        try
        {
            using Process process = new();
            process.StartInfo = new ProcessStartInfo(uri.AbsoluteUri)
            {
                UseShellExecute = true,
            };
            process.Start();
        }
        catch { }
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
