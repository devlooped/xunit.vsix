using System.IO;
using System.Windows;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
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

        service = GlobalServiceProvider.GetService<DTE>();
        Assert.NotNull(service);

        var hierarchy = GlobalServiceProvider.GetExport<IVsHierarchyItemManager>();

        Assert.NotNull(hierarchy);

        var items = GlobalServiceProvider.GetExports<ContentTypeDefinition>();

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
    public void when_reopenening_solution_then_vssolution_is_same()
    {
        var dte = ServiceProvider.GlobalProvider.GetService<DTE>();
        var solutionEmpty = ServiceProvider.GlobalProvider.GetService<SVsSolution, IVsSolution>();

        dte.Solution.Open(Path.Combine(BaseDir, "Content", "Library", "ClassLibrary.sln"));

        var solution1 = ServiceProvider.GlobalProvider.GetService<SVsSolution, IVsSolution>();

        dte.Solution.Close();
        dte.Solution.Open(Path.Combine(BaseDir, "Content", "Blank.sln"));

        var solution2 = ServiceProvider.GlobalProvider.GetService<SVsSolution, IVsSolution>();

        Assert.Same(solutionEmpty, solution1);
        Assert.Same(solution1, solution2);
        Assert.Same(solutionEmpty as IVsHierarchy, solution1 as IVsHierarchy);
        Assert.Same(solution1 as IVsHierarchy, solution2 as IVsHierarchy);
    }

    [VsixFact(RunOnUIThread = true)]
    public void when_reopenening_solution_then_hierarchy_item_is_same()
    {
        var dte = ServiceProvider.GlobalProvider.GetService<DTE>();
        var solutionEmpty = ServiceProvider.GlobalProvider.GetService<SVsSolution, IVsSolution>();
        var manager = ServiceProvider.GlobalProvider.GetService<SComponentModel, IComponentModel>().GetService<IVsHierarchyItemManager>();

        var solutionEmptyItem = manager.GetHierarchyItem(solutionEmpty as IVsHierarchy, (uint)VSConstants.VSITEMID.Root);
        Assert.NotNull(solutionEmptyItem);

        dte.Solution.Open(Path.Combine(BaseDir, "Content", "Blank.sln"));

        var solution1 = ServiceProvider.GlobalProvider.GetService<SVsSolution, IVsSolution>();
        var solution1Item = manager.GetHierarchyItem(solution1 as IVsHierarchy, (uint)VSConstants.VSITEMID.Root);

        dte.Solution.Close();

        dte.Solution.Open(Path.Combine(BaseDir, "Content", "Blank.sln"));

        var solution2 = ServiceProvider.GlobalProvider.GetService<SVsSolution, IVsSolution>();
        var solution2Item = manager.GetHierarchyItem(solution2 as IVsHierarchy, (uint)VSConstants.VSITEMID.Root);

        Assert.NotNull(solution1Item);
        Assert.NotNull(solutionEmptyItem);
        Assert.NotNull(solution2Item);

        Assert.Equal(solutionEmptyItem.HierarchyIdentity, solution1Item.HierarchyIdentity);
        Assert.Equal(solution1Item.HierarchyIdentity, solution2Item.HierarchyIdentity);
    }
}
