﻿using System.Runtime.InteropServices;

namespace PluginBurnMedia.Interop
{
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate void DFileSystemImageImport_EventHandler([In] [MarshalAs(UnmanagedType.IDispatch)] object sender,
        FsiFileSystems fileSystem, string currentItem, int importedDirectoryItems, int totalDirectoryItems,
        int importedFileItems, int totalFileItems);
}