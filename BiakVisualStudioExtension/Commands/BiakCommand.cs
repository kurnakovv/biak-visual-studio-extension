// Copyright (c) 2026 kurnakovv
// This file is licensed under the MIT License.
// See the LICENSE file in the project root for full license information.

using System;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace BiakVisualStudioExtension.Commands;

internal sealed class BiakCommand
{
    public const int COMMAND_ID = 0x0102;
    public static readonly Guid s_commandSet = new("4a9b5c6d-7e8f-4a1b-9c2d-3e4f5a6b7c8d");

    private BiakCommand(IMenuCommandService commandService)
    {
        if (commandService is null)
        {
            throw new ArgumentNullException(nameof(commandService));
        }

        CommandID menuCommandId = new(s_commandSet, COMMAND_ID);
        MenuCommand menuItem = new(Execute, menuCommandId);
        commandService.AddCommand(menuItem);
    }

    public static BiakCommand? Instance { get; private set; }

    public static async Task InitializeAsync(AsyncPackage package)
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);
        IMenuCommandService commandService = (await package.GetServiceAsync(typeof(IMenuCommandService)) as IMenuCommandService)
            ?? throw new InvalidOperationException($"Cannot get service {nameof(IMenuCommandService)}");
        Instance = new BiakCommand(commandService);
    }

    private void Execute(object sender, EventArgs e)
    {
        Console.WriteLine("Test");
    }
}
