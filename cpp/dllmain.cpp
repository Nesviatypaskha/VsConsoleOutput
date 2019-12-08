// dllmain.cpp : Defines the entry point for the DLL application.
#include "pch.h"
#include <exception>
//#include <windows.h> 
//#include <stdio.h>
//#include <tchar.h>
//#include <strsafe.h>
#include <iostream>
#include <fstream>
//#include <string>
#include <io.h>
//#include <stdlib.h>
#include <stdio.h>
#include <io.h>
//#include <fcntl.h>
//#include <process.h>


BOOL APIENTRY DllMain( HMODULE hModule,
                       DWORD  ul_reason_for_call,
                       LPVOID lpReserved
                     )
{
    switch (ul_reason_for_call)
    {
    case DLL_PROCESS_ATTACH:
    case DLL_THREAD_ATTACH:
    case DLL_THREAD_DETACH:
    case DLL_PROCESS_DETACH:
        break;
    }
    return TRUE;
}

__declspec(dllexport) void RedirectToPipe()
{
	std::cout << "RedirectToPipe()" << std::endl;
	try
	{
		HANDLE hPipe = CreateFile(TEXT("\\\\.\\pipe\\VSConsoleOutputBetaPipe"),
			GENERIC_WRITE,
			0,
			NULL,
			OPEN_ALWAYS,
			FILE_FLAG_DELETE_ON_CLOSE | FILE_ATTRIBUTE_NOT_CONTENT_INDEXED,
			NULL);
		if (hPipe != INVALID_HANDLE_VALUE)
		{
			int file_descriptor;
			file_descriptor = _open_osfhandle((intptr_t)hPipe, 0);
			if (file_descriptor != -1)
			{
				FILE* file;
				file = _fdopen(file_descriptor, "w");
				if (file != NULL)
				{
					if (_dup2(_fileno(file), 1) != -1)
					{

					}
					//std::cout.rdbuf(std::ofstream(file).rdbuf()); //redirect std::cout
				}
			}
		}
	}
	catch(std::exception & e)
	{
		std::cerr << "exception caught: " << e.what() << '\n';
	}
}
