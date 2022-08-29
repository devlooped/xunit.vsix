using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Utilities;
using Xunit.Abstractions;

namespace Xunit.Vsix.Tests;

public class Misc
{
    ITestOutputHelper _output;

    static readonly string BaseDir = Path.Combine(ThisAssembly.Project.MSBuildProjectDirectory);

    public Misc(ITestOutputHelper output)
    {
        _output = output;
    }

    [Trait("SanityCheck", "true")]
    [VsixFact(RunOnUIThread = true)]
    public async System.Threading.Tasks.Task SanityCheck()
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

        var service = ServiceProvider.GlobalProvider.GetService<DTE>();
        Assert.NotNull(service);

        var components = await ServiceProvider.GetGlobalServiceAsync<SComponentModel, IComponentModel>();

        var hierarchy = components.GetService<IVsHierarchyItemManager>();

        Assert.NotNull(hierarchy);

        var items = components.GetExtensions<ContentTypeDefinition>();

        Assert.NotEmpty(items);
    }

    [VsixFact]
    public async System.Threading.Tasks.Task when_executing_then_does_not_run_on_UI_thread()
    {
        var currentThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
        var uiThreadId = await Application.Current.Dispatcher.InvokeAsync(() => System.Threading.Thread.CurrentThread.ManagedThreadId);

        Assert.NotEqual(currentThreadId, uiThreadId);
    }

    [VsixFact(RunOnUIThread = true)]
    public async System.Threading.Tasks.Task when_requesting_ui_thread_then_runs_on_UI_thread()
    {
        var currentThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;

        Assert.Equal(currentThreadId, Application.Current.Dispatcher.Thread.ManagedThreadId);
    }

    [VsixFact(RunOnUIThread = true)]
    public async Task when_reopenening_solution_then_hierarchy_item_is_same()
    {
        var dte = await ServiceProvider.GetGlobalServiceAsync<SDTE, DTE>();
        var solutionEmpty = await ServiceProvider.GetGlobalServiceAsync<SVsSolution, IVsSolution>();
        var components = await ServiceProvider.GetGlobalServiceAsync<SComponentModel, IComponentModel>();

        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        var manager = components.GetService<IVsHierarchyItemManager>();

        var solutionEmptyItem = manager.GetHierarchyItem(solutionEmpty as IVsHierarchy, (uint)VSConstants.VSITEMID.Root);
        await TaskScheduler.Default;

        Assert.NotNull(solutionEmptyItem);

        dte.Solution.Open(Path.Combine(BaseDir, "Content", "Blank.sln"));

        var solution1 = await ServiceProvider.GetGlobalServiceAsync<SVsSolution, IVsSolution>();

        var solution1Item = manager.GetHierarchyItem(solution1 as IVsHierarchy, (uint)VSConstants.VSITEMID.Root);

        dte.Solution.Close();

        dte.Solution.Open(Path.Combine(BaseDir, "Content", "Blank.sln"));

        var solution2 = await ServiceProvider.GetGlobalServiceAsync<SVsSolution, IVsSolution>();
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        var solution2Item = manager.GetHierarchyItem(solution2 as IVsHierarchy, (uint)VSConstants.VSITEMID.Root);

        Assert.NotNull(solution1Item);
        Assert.NotNull(solutionEmptyItem);
        Assert.NotNull(solution2Item);

        Assert.Equal(solutionEmptyItem.HierarchyIdentity, solution1Item.HierarchyIdentity);
        Assert.Equal(solution1Item.HierarchyIdentity, solution2Item.HierarchyIdentity);
    }
}
