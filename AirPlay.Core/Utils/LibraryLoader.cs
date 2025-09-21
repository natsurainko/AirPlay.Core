﻿using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace AirPlay.Utils;

public static class LibraryLoader
{
    static LibraryLoader()
    {
        if (Environment.OSVersion.Platform == PlatformID.Unix || Environment.OSVersion.Platform == PlatformID.MacOSX || (int)Environment.OSVersion.Platform == 128)
        {
            // Theoretically needed only w/ linux
            LoadPosixLibrary();
        }
    }

    static void LoadPosixLibrary()
    {
        const int RTLD_NOW = 2;
        string rootDirectory = AppDomain.CurrentDomain.BaseDirectory;
        string assemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        var isOsx = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        var libFile = isOsx ? "libdl.dylib" : "libdl.so";

        var arch = (isOsx ? "osx" : "linux") + "-" + (Environment.Is64BitProcess ? "x64" : "x86");

        // Search a few different locations for our native assembly
        var paths = new[]
        {
            Path.Combine(rootDirectory, "runtimes", arch, "native", libFile),
            Path.Combine(rootDirectory, libFile),
            Path.Combine("/usr/local/lib", libFile),
            Path.Combine("/usr/lib", libFile)
        };

        foreach (var path in paths)
        {
            if (path == null)
            {
                continue;
            }

            if (File.Exists(path))
            {
                var addr = dlopen(path, RTLD_NOW);
                if (addr == IntPtr.Zero)
                {
                    var error = Marshal.PtrToStringAnsi(dlerror());
                    throw new Exception("dlopen failed: " + path + " : " + error);
                }

                return;
            }
        }

        if (!isOsx)
        {
            // Throw exception only with linux platform
            // OSX should works without libdl preload
            var error = "dlopen failed: unable to locate library " + libFile + ". Searched: " + paths.Aggregate((a, b) => a + "; " + b);
            Console.WriteLine(error);
            throw new Exception(error);
        }
    }

    public static IntPtr DlOpen(string fileName, int flags)
    {
        if (Environment.OSVersion.Platform == PlatformID.Unix || Environment.OSVersion.Platform == PlatformID.MacOSX || (int)Environment.OSVersion.Platform == 128)
        {
            return dlopen(fileName, flags);
        }
        else
        {
            var dllHandle = LoadLibrary(fileName);
            if (dllHandle == IntPtr.Zero)
            {
                var error = Marshal.GetLastWin32Error().ToString();
                // Code 193 - Arch error / ex: Win32 dll on Win64 error
                // Code 162 - Dependecy libraries not found
                Console.WriteLine($"Error 'LoadLibrary': {error}");
                throw new Exception(error);
            }

            return dllHandle;
        }
    }

    public static IntPtr DlSym(IntPtr handle, string symbol)
    {
        if (Environment.OSVersion.Platform == PlatformID.Unix || Environment.OSVersion.Platform == PlatformID.MacOSX || (int)Environment.OSVersion.Platform == 128)
        {
            return dlsym(handle, symbol);
        }
        else
        {
            return GetProcAddress(handle, symbol);
        }
    }

    public static IntPtr DlClose(IntPtr handle)
    {
        if (Environment.OSVersion.Platform == PlatformID.Unix || Environment.OSVersion.Platform == PlatformID.MacOSX || (int)Environment.OSVersion.Platform == 128)
        {
            return dlclose(handle);
        }
        else
        {
            return FreeLibrary(handle);
        }
    }

    // Used w/ Win32 & Win64

    [DllImport("kernel32.dll", EntryPoint = "LoadLibrary", SetLastError = true)]
    private static extern IntPtr LoadLibrary(string filename);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetProcAddress(IntPtr handle, string symbol);

    [DllImport("kernel32.dll")]
    private static extern IntPtr FreeLibrary(IntPtr handle);

    // Used w/ OSX & Linux

    [DllImport("libdl")]
    private static extern IntPtr dlopen(string fileName, int flags);

    [DllImport("libdl")]
    private static extern IntPtr dlerror();

    [DllImport("libdl")]
    private static extern IntPtr dlsym(IntPtr handle, string symbol);

    [DllImport("libdl")]
    private static extern IntPtr dlclose(IntPtr handle);
}
