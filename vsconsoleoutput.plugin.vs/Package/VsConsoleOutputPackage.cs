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
    [InstalledProductRegistration("VsConsoleOutput", "VsConsoleOutput", "1.0.3", IconResourceID = 400)]
    public sealed class VSConsoleOutputPackage : AsyncPackage
    {
        public const string PackageGuidString = "f6dfad00-7979-4fd7-b28b-71336c51f20f";
        private static EnvDTE.DTE s_DTE = null;

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            try
            {
                service.Debug.Initialize();
            }
            catch (Exception ex)
            {
                service.Output.WriteError(ex.ToString());
            }
        }

        protected override int QueryClose(out bool canClose)
        {
            {
                canClose = true;
            }
            try
            {
                service.Debug.Finalize();
                ErrorHandler.ThrowOnFailure(base.QueryClose(out canClose));
            }
            catch (Exception ex) 
            {
                service.Output.WriteError(ex.ToString());
            }
            return VSConstants.S_OK;
        }

        public static EnvDTE.DTE GetDTE()
        {
            if (s_DTE == null)
            {
                s_DTE = Package.GetGlobalService(typeof(SDTE)) as EnvDTE.DTE;
            }
            return s_DTE;
        }
    }
}
