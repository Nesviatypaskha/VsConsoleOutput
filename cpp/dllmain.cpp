#include "pch.h"
#include <exception>
#include <iostream>
#include <fstream>
#include <io.h>
#include <stdio.h>

__declspec(dllexport) void RedirectToPipe()
{
	try
	{
		HANDLE hPipe = CreateFile(TEXT("\\\\.\\pipe\\VSConsoleOutputPipe"),
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
					std::cout << "Console redirected to Output Window in Visual Studio" << std::endl;
					if (_dup2(_fileno(file), 1) != -1)
					{
						std::cout << "Console redirected to Output Window in Visual Studio" << std::endl;
					}
				}
			}
		}
	}
	catch(std::exception & e)
	{
		std::cerr << "exception caught: " << e.what() << '\n';
	}
}

BOOL APIENTRY DllMain(HMODULE hModule,
	DWORD  ul_reason_for_call,
	LPVOID lpReserved
)
{
	switch (ul_reason_for_call)
	{
	case DLL_PROCESS_ATTACH:
		RedirectToPipe();
		break;
	case DLL_THREAD_ATTACH:
	case DLL_THREAD_DETACH:
	case DLL_PROCESS_DETACH:
		break;
	}
	return TRUE;
}
