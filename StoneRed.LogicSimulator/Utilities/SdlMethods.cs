using System;
using System.Runtime.InteropServices;

namespace StoneRed.LogicSimulator.Utilities;

internal static class SdlMethods
{
    private const string NativeLibName = "SDL2.dll";

    [DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "SDL_MaximizeWindow")]
    public static extern void MaximizeWindow(IntPtr window);

    [DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "SDL_MinimizeWindow")]
    public static extern void MinimizeWindow(IntPtr window);

    [DllImport(NativeLibName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "SDL_RestoreWindow")]
    public static extern void RestoreWindow(IntPtr window);
}