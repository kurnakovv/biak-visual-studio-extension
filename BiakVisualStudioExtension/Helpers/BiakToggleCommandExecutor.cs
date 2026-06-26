// Copyright (c) 2026 kurnakovv
// This file is licensed under the MIT License.
// See the LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;

namespace BiakVisualStudioExtension.Helpers;

internal static class BiakToggleCommandExecutor
{
    public static async Task ExecuteAsync(string arguments)
    {
        try
        {
            (int exitCode, string standardOutput, string standardError) = await RunProcessAsync(arguments);

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            string message = string.IsNullOrWhiteSpace(standardError)
                ? standardOutput.Trim()
                : standardError.Trim();

            if (message.Contains("Biak is not initialized.") && message.Contains("Please run: dotnet biak setup"))
            {
                ToastNotification.Show(
                    "Biak",
                    message,
                    "https://github.com/kurnakovv/biak/wiki/Setup",
                    ToastIconKind.Warning
                );
                return;
            }

            if (exitCode == 0 && string.IsNullOrWhiteSpace(standardError))
            {
                string successMessage = string.IsNullOrWhiteSpace(message) ? "The command was executed successfully." : message;
                await ShowSuccessNotificationAsync(successMessage);
                return;
            }

            if (IsBiakNotInstalledError(standardError))
            {
                ToastNotification.Show(
                    "Biak",
                    "dotnet biak is not installed. Install the tool and try again.",
                    "https://github.com/kurnakovv/biak",
                    ToastIconKind.Error
                );
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
        catch (Exception ex)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            VsShellUtilities.ShowMessageBox(
                ServiceProvider.GlobalProvider,
                ex.Message,
                "Biak",
                OLEMSGICON.OLEMSGICON_CRITICAL,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }
    }

    private static async Task<(int ExitCode, string StandardOutput, string StandardError)> RunProcessAsync(string arguments)
    {
        using Process process = new();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = "biak " + arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        process.Start();
        Task<string> stdoutTask = process.StandardOutput.ReadToEndAsync();
        Task<string> stderrTask = process.StandardError.ReadToEndAsync();

        await Task.WhenAll(
            stdoutTask,
            stderrTask,
            process.WaitForExitAsync()
        );

        string standardOutput = await stdoutTask;
        string standardError = await stderrTask;

        return (process.ExitCode, standardOutput, standardError);
    }

    private static bool IsBiakNotInstalledError(string standardError)
    {
        if (string.IsNullOrWhiteSpace(standardError))
        {
            return false;
        }

        string normalizedError = standardError.ToLowerInvariant();
        return normalizedError.Contains("dotnet-biak")
            || normalizedError.Contains("could not execute because the specified command or file was not found")
            || normalizedError.Contains("no executable found matching command");
    }

    private static async Task ShowSuccessNotificationAsync(string message)
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        ToastNotification.Show("Biak", message, ToastIconKind.Success);
        await ShowStatusBarMessageAsync(message);
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
        _ = statusBar.SetText(string.Empty);
    }
}
