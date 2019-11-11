using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VsConsoleOutput
{
    class Info
    {
        //        Launching a Program
        //Users who want to debug a program can press F5 to run the debugger from theIDE.This begins a series of events that
        //ultimately result in theIDE's connecting to a debug engine(DE), which is in turn connected, or attached, to the program as
        //follows:
        //1. TheIDE first calls the project packageto get thesolution's active project debug settings.Thesettings includethestarting
        //directory, theenvironmentvariables, the port in which the program will run,and the DE to useto createthe program, if
        //specified.Thesesettings are passed to the debug package.
        //2. If a DE is specified, the DE calls the operating system to launch the program.As a consequence of launching the program,
        //the program's run-timeenvironment is loaded.For example, if a program is written in MSIL, thecommon language
        //runtime will beinvoked to run the program.
        //-orIf a DE is not specified, the port calls the operating system to launch the program, which causes the program's run-time
        //environment to beloaded.
        //Note
        //If a DE is used to launch a program, it is likely that thesame DE will beattached to the program.
        //3. Depending on whether the DE or the port launched the program, the DE or therun-timeenvironment then creates a
        //program description, or node, and notifies the port that the program is running.
        //Note
        //It is recommended that therun-timeenvironment createthe program node, becausethe program nodeis a lightweight
        //representation of a program that can be debugged.Thereis no need to load up an entire DE just to createand register
        //a program node. If the DE is designed to run in the process of theIDE, but no IDE is actually running, there needs to be
        //a component that can add a program nodeto the port.
        //The newly created program, along with any other programs, related or unrelated, launched or attached to from thesameIDE,
        //composea debug session.
        //Programmatically, when the user first presses F5, Visual Studio's debug packagecalls the project package(which is associated
        //with thetype of program being launched) through the DebugLaunch method, which in turn fills outa VsDebugTargetInfo2
        //structure with thesolution's active project debug settings.This structureis passed back to the debug packagethrough a call to
        //theLaunchDebugTargets2 method.The debug packagethen instantiates thesession debug manager (SDM), which launches
        //the program being debugged and any associated debug engines.
        //One of thearguments passed to theSDM is the GUID of the DE to be used to launch the program.
        //If the DE GUID is not GUID_NULL, theSDM co-creates the DE, and then calls its IDebugEngineLaunch2::LaunchSuspended
        //method to launch the program.For example, if a program is written in nativecode, then
        //IDebugEngineLaunch2::LaunchSuspended will probably call CreateProcess and ResumeThread (Win32 functions) to run
        //the program.
        //As a consequence of launching the program, the program's run-timeenvironment is loaded.Either the DE or therun-time
        //environment then creates an IDebugProgramNode2 interfaceto describethe program and passes this interfaceto
        //IDebugPortNotify2::AddProgramNodeto notify the port that the program is running.
        //If GUID_NULLis passed, then the port launches the program. Oncethe program is running, therun-timeenvironment creates
        //an IDebugProgramNode2 interfaceto describethe program and passes it to IDebugPortNotify2::AddProgramNode.This
        //notifies the port that the program is running.Then theSDM attaches the debug engineto therunning program.
        //In This Section
        //Notifying the Port
        //Explains what happens after a program is launched and the port is notified.
        //Attaching After a Launch
        //Documents when the debug session is ready to attach the DE to the program.
        //Documents when the debug session is ready to attach the DE to the program.
        //Related Sections
        //Debugging Tasks
        //Contains links to various debugging tasks, such as launching a program and evaluating expressions



        //        Notifying the Port
        //        After launching a program, the port must be notified,as follows:
        //1. When a port receives a new program node, it sends a program creation event back to the debug session.Theevent
        //carries with itan interfacethat represents the program.
        //2. The debug session queries the program for theidentifier of a debug engine(DE) that can attach to.
        //3. The debug session checks to seeif the DE is on thelist of allowable DEs for that program.The debug session gets this list
        //from thesolution's active program settings, originally passed to it by the debug package.
        //The DE must be on theallowablelist, or elsethe DE will not beattached to the program.
        //Programmatically, when a port first receives a new program node, it creates an IDebugProgram2 interfaceto represent the
        //program.
        //Note
        //This should not beconfused with theIDebugProgram2 interfacecreated later by the debug engine(DE).
        //The port sends an IDebugProgramCreateEvent2 program creation event back to thesession debug manager(SDM) by means
        //of a COM IConnectionPoint interface.
        //Note
        //This should not beconfused with theIDebugProgramCreateEvent2 interface, which is sent later by the DE.
        //Discussion
        //Along with theevent interfaceitself, the port sends theIDebugPort2, IDebugProcess2, and IDebugProgram2 interfaces, which
        //represent the port, process,and program, respectively.TheSDM calls IDebugProgram2::GetEngineInfo to get the GUID of the
        //DE that can debug the program.The GUID was originally obtained from theIDebugProgramNode2 interface.
        //TheSDM checks to seeif the DE is on thelist of allowable DEs.TheSDM gets this list from thesolution's active program
        //settings, originally passed to it by the debug package.The DE must be on theallowablelist, or elseit will not beattached to the
        //program.
        //Oncetheidentity of the DE is known, theSDM is ready to attach it to the program.
        //See Also
        //Concepts
        //Launching a Program
        //Attaching After a Launch
        //Debugging Tasks

        //--------------------------------------------------------------------------------------------------------------------------------------------
        //IVsDebugProcessNotify Provides notice that the debugger is about to stop.Used as the
        //VsDebugTargetInfo2 argument in the LaunchDebugTargets2 method of the
        //IVsDebugger2 interface

        //--------------------------------------------------------------------------------------------------------------------------------------------
        //DEBUG_LAUNCH_OPERATION The DEBUG_LAUNCH_OPERATION enumeration is a member of the
        //VsDebugTargetInfo structure, a parameter of LaunchDebugTargets calls

        //--------------------------------------------------------------------------------------------------------------------------------------------
        //IVsDebugger Interface
        //Provides access to the current debugger so that the package can listen for debugger events.You can get an instance of this
        //interface from the GetIVsDebugger method of the LanguageService service.
        //Namespace: Microsoft.VisualStudio.Shell.Interop
        //Assembly: Microsoft.VisualStudio.Shell.Interop(in microsoft.visualstudio.shell.interop.dll)
        //Syntax
        //VB
        //C#
        //C++
        //J#
        //JScript
        //Remarks
        //The DebugLaunch can add or modify parameters passed to the LaunchDebugTargets to, for example, launch a custom debug
        //engine.
        //For examples of using the interface, see the code for Using the Babel Package, the Figures Sample, and My C Package Sample.
        //Notes to Implementers The environment implements this interface.
        //Notes to Callers This interface is used by DebugLaunch

        //--------------------------------------------------------------------------------------------------------------------------------------------
        //IVsDebugger.LaunchDebugTargets Method
        //Launches or attaches to the specified processes under the control of the debugger.
        //Parameters
        //cTargets
        //[in] Number of targets to launch(specifies the number of VsDebugTargetInfo structures pointed to
        //rgDebugTargetInfo
        //[in, out] Array of VsDebugTargetInfo structures describing the programs to launch or attach to


        //--------------------------------------------------------------------------------------------------------------------------------------------
        //Attaching After a Launch
        //After a program has been launched, the debug session is ready to attach the debug engine(DE) to said program.
        //Discussion
        //Because communication is easier within a shared address space, you must decide whether it makes more sense to facilitate the
        //communication between the debug session and the DE, or between the DE and the program.Choose between the following:
        //If it makes more sense to facilitate communication between the debug session and the DE, then the debug session cocreates the DE and asks the DE to attach to the program. This leaves the debug session and DE together in one address
        //space, and the run-time environment and program together in another.
        //If it makes more sense to facilitate communication between the DE and the program, then the run-time environment cocreates the DE.This leaves the SDM in one address space, and the DE, run-time environment, and program together in
        //another.This is typical of a DE that is implemented with an interpreter to run scripted languages.
        //Note
        //How the DE attaches to the program is implementation-dependent.Communication between the DE and the program i
        //s also implementation-dependent.
        //Programmatically, when the session debug manager (SDM) first receives the IDebugProgram2 object that represents the
        //program to be launched, it calls the IDebugProgram2::Attach method, passing it an IDebugEventCallback2 object, which is later
        //used to pass debug events back to the SDM. The IDebugProgram2::Attach method then calls the
        //IDebugProgramNodeAttach2::OnAttach method. For more information on how the SDM receives the IDebugProgram2
        //interface, see Notifying the Port.
        //If your DE needs to run in the same address space as the program being debugged, typically because the DE is part of an
        //interpreter running a script, the IDebugProgramNodeAttach2::OnAttach method returns S_FALSE, indicating that it
        //completed the attach process.
        //If, on the other hand, the DE runs in the address space of the SDM, the IDebugProgramNodeAttach2::OnAttach method
        //returns S_OK or the IDebugProgramNodeAttach2 interface is not implemented at all on the IDebugProgramNode2 object
        //associated with the program being debugged.In this case, the IDebugEngine2::Attach method is eventually called to complete
        //the attach operation.
        //In the latter case, you must call the IDebugProgram2::GetProgramId method on the IDebugProgram2 object that was passed
        //to the IDebugEngine2::Attach method, store the GUID in the local program object, and return this GUID when the
        //IDebugProgram2::GetProgramId method is subsequently called on this object. The GUID is used to identify the program
        //uniquely across the various debug components.
        //Note that in the case of the IDebugProgramNodeAttach2::OnAttach method returning S_FALSE, the GUID to use for the
        //program is passed to that method and it is the IDebugProgramNodeAttach2::OnAttach method that sets the GUID on the
        //local program object.
        //The DE is now attached to the program and ready to send any startup events.
        //See Also
        //Reference
        //IDebugEventCallback2
        //IDebugProgram2
        //IDebugProgram2::Attach
        //IDebugProgram2::GetProgramId
        //IDebugProgramNode2
        //IDebugProgramNodeAttach2
        //IDebugProgramNodeAttach2::OnAttach
        //IDebugEngine2::Attach
        //Concepts
        //Attaching Directly to a Program
        //Notifying the Port
        //Debugging Tasks

        //--------------------------------------------------------------------------------------------------------------------------------------------
        //Sending Startup Events After a Launch
        //Once the debug engine(DE) is attached to the program, it sends a series of startup events back to the debug session.
        //Discussion
        //Startup events sent back to the debug session include the following:
        //An engine creation event.
        //A program creation event.
        //Thread creation and module load events.
        //A load complete event, sent when the code is loaded and ready to run, but before any code is executed
        //Note
        //When this event is continued, global variables are initialized and startup routines run.
        //Possible other thread creation and module load events.
        //An entry point event, which signals that the program has reached its main entry point, such as Main or WinMain. This
        //event is not typically sent if the DE attaches to a program that is already running.
        //Programmatically, the DE first sends the session debug manager (SDM) an IDebugEngineCreateEvent2 interface, which
        //represents an engine creation event, followed by an IDebugProgramCreateEvent2, which represents a program creation event.
        //This is typically followed by one or more IDebugThreadCreateEvent2 thread creation events and IDebugModuleLoadEvent2
        //module load events.
        //When the code is loaded and ready to run, but before any code is executed, the DE sends the SDM an
        //IDebugLoadCompleteEvent2 load complete event. Finally, if the program is not already running, the DE sends an
        //IDebugEntryPointEvent2 entry point event, signaling that the program has reached its main entry point and is ready for
        //debugging.
        //See Also
        //Concepts
        //Control of Execution
        //Debugging Tasks
    }
}
