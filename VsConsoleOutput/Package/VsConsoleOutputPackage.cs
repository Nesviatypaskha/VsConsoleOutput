using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using Task = System.Threading.Tasks.Task;

namespace package
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid(VSConsoleOutputPackage.PackageGuidString)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.FirstLaunchSetup_string, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.ShellInitialized_string, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionExists_string, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionHasMultipleProjects_string, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionHasSingleProject_string, PackageAutoLoadFlags.BackgroundLoad)]

    [InstalledProductRegistration("VSConsoleOutput", "VSConsoleOutput", "0.9.1", IconResourceID = 400)]
    public sealed class VSConsoleOutputPackage : AsyncPackage
    {
        public const string PackageGuidString = "f6dfad00-7979-4fd7-b28b-71336c51f20f";

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            service.Debug.Initialize();
            service.Solution.Initialize();

        }

        protected override int QueryClose(out bool canClose)
        {
            int hr = VSConstants.S_OK;
            {
                canClose = true;
            }
            try
            {
                service.Debug.Finalize();
                hr = base.QueryClose(out canClose);
                Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(hr);
            }
            catch (Exception ex) 
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }
            return hr;
        }
    }
}
