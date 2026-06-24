// Copyright (c) 2026 kurnakovv
// This file is licensed under the MIT License.
// See the LICENSE file in the project root for full license information.

using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace BiakVisualStudioExtension.Commands;

internal sealed class DisableCommand
{
    public const int COMMAND_ID = 0x0101;
    public static readonly Guid s_commandSet = new("4a9b5c6d-7e8f-4a1b-9c2d-3e4f5a6b7c8d");
    private static NotifyIcon? s_notifyIcon;

    private DisableCommand(IMenuCommandService commandService)
    {
        if (commandService is null)
        {
            throw new ArgumentNullException(nameof(commandService));
        }

        CommandID menuCommandId = new(s_commandSet, COMMAND_ID);
        MenuCommand menuItem = new(Execute, menuCommandId);
        commandService.AddCommand(menuItem);
    }

    public static DisableCommand? Instance { get; private set; }

    public static async Task InitializeAsync(AsyncPackage package)
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);
        IMenuCommandService commandService = (await package.GetServiceAsync(typeof(IMenuCommandService)) as IMenuCommandService)
            ?? throw new InvalidOperationException($"Cannot get service {nameof(IMenuCommandService)}");
        Instance = new DisableCommand(commandService);
    }

    private void Execute(object sender, EventArgs e)
    {
        _ = RunBiakAsync();
    }

    private static async Task RunBiakAsync()
    {
        (int exitCode, string standardOutput, string standardError) = await Task.Run(
            () =>
            {
                using Process process = new();
                process.StartInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = "biak disable",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };

                process.Start();
                string standardOutput = process.StandardOutput.ReadToEnd();
                string standardError = process.StandardError.ReadToEnd();
                process.WaitForExit();

                return (process.ExitCode, standardOutput, standardError);
            }
        );

        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

        string message = string.IsNullOrWhiteSpace(standardError)
            ? standardOutput.Trim()
            : standardError.Trim();

        if (exitCode == 0 && string.IsNullOrWhiteSpace(standardError))
        {
            await ShowSuccessNotificationAsync(string.IsNullOrWhiteSpace(message) ? "Biak disabled successfully." : message);
            return;
        }

        VsShellUtilities.ShowMessageBox(
            ServiceProvider.GlobalProvider,
            string.IsNullOrWhiteSpace(message) ? "The command completed with an error." : message,
            "Biak",
            OLEMSGICON.OLEMSGICON_CRITICAL,
            OLEMSGBUTTON.OLEMSGBUTTON_OK,
            OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
    }

    private static async Task ShowSuccessNotificationAsync(string message)
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        ShowTrayNotification(message);
        await ShowStatusBarMessageAsync(message);
        DismissTrayNotification();
    }

    private static void ShowTrayNotification(string message)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        s_notifyIcon?.Dispose();
        s_notifyIcon = new NotifyIcon
        {
            Icon = SystemIcons.Information,
            Visible = true,
            BalloonTipTitle = "Biak",
            BalloonTipText = message,
            BalloonTipIcon = ToolTipIcon.Info,
        };

        s_notifyIcon.ShowBalloonTip(3000);
    }

    private static void DismissTrayNotification()
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        s_notifyIcon?.Dispose();
        s_notifyIcon = null;
    }

    private static async Task ShowStatusBarMessageAsync(string message)
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

        if (ServiceProvider.GlobalProvider.GetService(typeof(SVsStatusbar)) is not IVsStatusbar statusBar)
        {
            return;
        }

        _ = statusBar.SetText(message);

        await Task.Delay(4000);
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

        if (ServiceProvider.GlobalProvider.GetService(typeof(SVsStatusbar)) is IVsStatusbar statusBarToClear)
        {
            _ = statusBarToClear.SetText(string.Empty);
        }
    }
}
