#include <exception>
#include <iostream>
#include <fstream>
#include <io.h>
#include <stdio.h>
#include <windows.h>
#include <fcntl.h>

extern "C"
{
    extern __declspec(dllexport) int test()
    {
        std::cout << "test..." << std::endl;
        return 42;
    }
}

namespace proxy {

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

            if (s_Pipe == INVALID_HANDLE_VALUE)
            {
                std::cerr << "ERROR : Console don't redirected because pipe not created" << std::endl;
                return;
            }

            std::cout << "Console redirected to Output Window in Visual Studio..." << std::endl;

            auto a_Context = _open_osfhandle(( intptr_t )s_Pipe, _O_RDWR);

            if (a_Context == -1)
            {
                std::cerr << "ERROR: Console don't redirected because handle not created" << std::endl;
                return;
            }
            auto a_Context1 = _fdopen(a_Context, "w");

            if (a_Context1 == NULL)
            {
                std::cerr << "ERROR: Console don't redirected because pipe not opened" << std::endl;
                return;
            }

            if (_dup2(_fileno(a_Context1), 1) != 0)
            {
                std::cerr << "ERROR: Console don't redirected correctly (Error code: " << errno << ")" << std::endl;
                return;
            }

            setvbuf(stdout, NULL, _IONBF, 0);
        }
        catch (std::exception& ex)
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
    if (reason == DLL_PROCESS_ATTACH)
    {
        proxy::Redirection::Connect();
    }
    return TRUE;
}
