#include <exception>
#include <iostream>
#include <fstream>
#include <io.h>
#include <stdio.h>
#include <windows.h>

namespace proxy
{
    class Redirection
    {
    public:
        static void Connect()
        {
            try
            {
                s_Pipe = CreateFile(
                    TEXT("\\\\.\\pipe\\VsConsoleOutput"),
                    GENERIC_WRITE,
                    0,
                    NULL,
                    OPEN_ALWAYS,
                    FILE_FLAG_DELETE_ON_CLOSE | FILE_ATTRIBUTE_NOT_CONTENT_INDEXED,
                    NULL);
                if (s_Pipe != INVALID_HANDLE_VALUE)
                {
                    {
                        std::cout << "Console redirected to Output Window in Visual Studio..." << std::endl;
                    }
                    {
                        auto a_Context = _open_osfhandle((intptr_t)s_Pipe, 0);
                        if (a_Context != -1)
                        {
                            auto a_Context1 = _fdopen(a_Context, "w");
                            if (a_Context1 != NULL)
                            {
                                if (_dup2(_fileno(a_Context1), 1) != 0)
                                {
                                    std::cerr << "ERROR: Console don't redirected correctly (Error code: " << errno << ")" << std::endl;
                                }
                            }
                            else
                            {
                                std::cerr << "ERROR: Console don't redirected because pipe not opened" << std::endl;
                            }
                        }
                        else
                        {
                            std::cerr << "ERROR: Console don't redirected because handle not created" << std::endl;
                        }
                    }
                }
                else
                {
                    std::cerr << "ERROR: Console don't redirected because pipe not created" << std::endl;
                }
            }
            catch (std::exception & ex)
            {
                std::cerr << "ERROR: " << ex.what() << std::endl;
            }
        }
    public:
        static void Disconnect()
        {
            if (s_Pipe != INVALID_HANDLE_VALUE)
            {
                CloseHandle(s_Pipe);
            }
        }
    private:
        static HANDLE s_Pipe;
    };
}

HANDLE proxy::Redirection::s_Pipe = INVALID_HANDLE_VALUE;

BOOL APIENTRY DllMain(HMODULE module, DWORD reason, LPVOID reserved)
{
    switch (reason)
    {
    case DLL_PROCESS_ATTACH:
        proxy::Redirection::Connect();
        break;
    case DLL_PROCESS_DETACH:
        proxy::Redirection::Disconnect();
        break;
    }
    return TRUE;
}
