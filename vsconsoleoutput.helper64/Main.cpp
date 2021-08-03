#include "WinReg.hpp"
#include <exception>
#include <iostream>
#include <fstream> 

int main()
{
    try
    {
        //TODO Check is system x64?
        
        std::ofstream outfile("test.txt");

        outfile << "my text here!" << std::endl;

        outfile.close();

        const std::wstring VSConsoleOutputSubKey = L"SOFTWARE\\VSConsoleOutput";
        winreg::RegKey key(HKEY_CURRENT_USER, VSConsoleOutputSubKey);

        key.Open(HKEY_CURRENT_USER, VSConsoleOutputSubKey);

        HINSTANCE hModule = NULL; 
        typedef  BOOL(WINAPI MESS)(UINT);

        hModule = ::LoadLibrary(L"kernel32.dll");
        ULONGLONG vLoadLibraryA = 0;
        ULONGLONG vGetProcAddress = 0;
        if (hModule != NULL)
        {
            vLoadLibraryA = (ULONGLONG)GetProcAddress((HMODULE)hModule, "LoadLibraryA");
            vGetProcAddress = (ULONGLONG)GetProcAddress((HMODULE)hModule, "GetProcAddress");
            ::FreeLibrary(hModule);
        }

        key.SetQwordValue(L"LoadLibraryA_x64", vLoadLibraryA);
        key.SetQwordValue(L"GetProcAddress_x64", vGetProcAddress);

    }
    catch (const std::exception& e)
    {
        std::wcout << L"\n*** ERROR: " << e.what() << L'\n';
    }
    return 0; 
}