using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Timers;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace VSConsoleOutputBeta
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the
    /// IVsPackage interface and uses the registration attributes defined in the framework to
    /// register itself and its components with the shell. These attributes tell the pkgdef creation
    /// utility what data to put into .pkgdef file.
    /// </para>
    /// <para>
    /// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
    /// </para>
    /// </remarks>
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid(VSConsoleOutputBetaPackage.PackageGuidString)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.FirstLaunchSetup_string, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.ShellInitialized_string, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionExists_string, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionHasMultipleProjects_string, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionHasSingleProject_string, PackageAutoLoadFlags.BackgroundLoad)]

    [InstalledProductRegistration("VSConsoleOutputBeta", "VSConsoleOutputBeta", "0.1.0", IconResourceID = 400)]
    public sealed class VSConsoleOutputBetaPackage : AsyncPackage
    {
        /// <summary>
        /// VSConsoleOutputBetaPackage GUID string.
        /// </summary>
        public const string PackageGuidString = "f6dfad00-7979-4fd7-b28b-71336c51f20f";
        private static IVsDebugger _debugger;
        private static CancellationToken _cancellationToken;
        private System.Threading.Thread serverThread;
        private static DTE _dte;
        private static DTE2 _dte2;
        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to monitor for initialization cancellation, which can occur when VS is shutting down.</param>
        /// <param name="progress">A provider for progress updates.</param>
        /// <returns>A task representing the async work of package initialization, or an already completed task if there is none. Do not return null from this method.</returns>
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            // When initialized asynchronously, the current thread may be a background thread at this point.
            // Do any initialization that requires the UI thread after switching to the UI thread.
            await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            DebugManager.Instantiate();
            DebugManager.Instance.Advise();
            //resource.service.Solution.Connect();
            //Output.Initialize();

            //serverThread = new System.Threading.Thread(Pipes.StartServer);
            //serverThread.Start();
        }

        public static DTE getDTE()
        {
            if (_dte == null)
                _dte = GetGlobalService(typeof(SDTE)) as DTE;
            return _dte;
        }
        public static DTE2 getDTE2()
        {
            if (_dte2 == null)
                _dte2 = GetGlobalService(typeof(SDTE)) as DTE2;
            return _dte2;
        }

        public static IVsDebugger getDebugger()
        {
            if (_debugger == null)
                _debugger = GetGlobalService(typeof(SVsShellDebugger)) as IVsDebugger;
            return _debugger;
        }

        protected override int QueryClose(out bool canClose)
        {
            int hr = VSConstants.S_OK;
            canClose = true;
            try
            {
                DebugManager.Instance.Unadvise();
                hr = base.QueryClose(out canClose);
                Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(hr);
            }
            catch (Exception ex)
            {
                // TODO: catch
            }
            return hr;
        }
        #endregion
    }
}
