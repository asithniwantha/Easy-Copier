using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Easy_Copier.Interop
{
    /// <summary>
    /// Provides interop helper constants and P/Invoke declarations for Windows Shell file operations.
    /// </summary>
    public static class FileOperationInterop
    {
        public const string CLSID_FileOperation = "3AD05575-8857-4850-9277-11B85BDB8E09";

        /// <summary>
        /// Creates and initializes a Shell item object from a parsing name.
        /// </summary>
        [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
        internal static extern void SHCreateItemFromParsingName(
            [In][MarshalAs(UnmanagedType.LPWStr)] string pszPath,
            [In] IntPtr pbc,
            [In][MarshalAs(UnmanagedType.LPStruct)] Guid riid,
            [Out][MarshalAs(UnmanagedType.Interface)] out IShellItem ppv);
    }

    /// <summary>
    /// Exposes methods for copying, moving, renaming, deleting, and creating Shell items.
    /// </summary>
    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("947aab5f-0a5c-4c13-b4d6-4bf7836fc9f8")]
    public interface IFileOperation
    {
        [PreserveSig] uint Advise(IFileOperationProgressSink sink, out uint pdwCookie);
        [PreserveSig] uint Unadvise(uint dwCookie);
        [PreserveSig] uint SetOperationFlags(FILEOP_FLAGS dwOperationFlags);
        [PreserveSig] uint SetProgressMessage([MarshalAs(UnmanagedType.LPWStr)] string pszMessage);
        [PreserveSig] uint SetProgressDialog([In] IntPtr popd);
        [PreserveSig] uint SetProperties([In] IntPtr pproparray);
        [PreserveSig] uint SetOwnerWindow(IntPtr hwndOwner);
        [PreserveSig] uint ApplyPropertiesToItem(IShellItem psiItem);
        [PreserveSig] uint ApplyPropertiesToItems([In] IntPtr punkItems);
        [PreserveSig] uint RenameItem(IShellItem psiItem, [MarshalAs(UnmanagedType.LPWStr)] string pszNewName, IFileOperationProgressSink? pfopsItem);
        [PreserveSig] uint RenameItems([In] IntPtr pUnkItems, [MarshalAs(UnmanagedType.LPWStr)] string pszNewName);
        [PreserveSig] uint MoveItem(IShellItem psiItem, IShellItem psiDestinationFolder, [MarshalAs(UnmanagedType.LPWStr)] string? pszNewName, IFileOperationProgressSink? pfopsItem);
        [PreserveSig] uint MoveItems([In] IntPtr punkItems, IShellItem psiDestinationFolder);
        [PreserveSig] uint CopyItem(IShellItem psiItem, IShellItem psiDestinationFolder, [MarshalAs(UnmanagedType.LPWStr)] string? pszCopyName, IFileOperationProgressSink? pfopsItem);
        [PreserveSig] uint CopyItems([In] IntPtr punkItems, IShellItem psiDestinationFolder);
        [PreserveSig] uint DeleteItem(IShellItem psiItem, IFileOperationProgressSink? pfopsItem);
        [PreserveSig] uint DeleteItems([In] IntPtr punkItems);
        [PreserveSig] uint NewItem(IShellItem psiDestinationFolder, [In] IntPtr dwFileAttributes, [MarshalAs(UnmanagedType.LPWStr)] string pszName, [MarshalAs(UnmanagedType.LPWStr)] string? pszTemplateName, IFileOperationProgressSink? pfopsItem);
        [PreserveSig] uint PerformOperations();
        [PreserveSig] uint GetAnyOperationsAborted(out bool pfAnyOperationsAborted);
    }

    /// <summary>
    /// Exposes status and notification methods for file operation events.
    /// </summary>
    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("04b0f1a5-8d70-48ea-a083-d161a65492d4")]
    public interface IFileOperationProgressSink
    {
        [PreserveSig] uint StartOperations();
        [PreserveSig] uint FinishOperations(uint hrResult);
        [PreserveSig] uint PreRenameItem(uint dwFlags, IShellItem psiItem, [MarshalAs(UnmanagedType.LPWStr)] string pszNewName);
        [PreserveSig] uint PostRenameItem(uint dwFlags, IShellItem psiItem, [MarshalAs(UnmanagedType.LPWStr)] string pszNewName, uint hrRename, IShellItem psiNewlyCreated);
        [PreserveSig] uint PreMoveItem(uint dwFlags, IShellItem psiItem, IShellItem psiDestinationFolder, [MarshalAs(UnmanagedType.LPWStr)] string pszNewName);
        [PreserveSig] uint PostMoveItem(uint dwFlags, IShellItem psiItem, IShellItem psiDestinationFolder, [MarshalAs(UnmanagedType.LPWStr)] string pszNewName, uint hrMove, IShellItem psiNewlyCreated);
        [PreserveSig] uint PreCopyItem(uint dwFlags, IShellItem psiItem, IShellItem psiDestinationFolder, [MarshalAs(UnmanagedType.LPWStr)] string pszNewName);
        [PreserveSig] uint PostCopyItem(uint dwFlags, IShellItem psiItem, IShellItem psiDestinationFolder, [MarshalAs(UnmanagedType.LPWStr)] string pszNewName, uint hrCopy, IShellItem psiNewlyCreated);
        [PreserveSig] uint PreDeleteItem(uint dwFlags, IShellItem psiItem);
        [PreserveSig] uint PostDeleteItem(uint dwFlags, IShellItem psiItem, uint hrDelete, IShellItem psiNewlyCreated);
        [PreserveSig] uint PreNewItem(uint dwFlags, IShellItem psiDestinationFolder, [MarshalAs(UnmanagedType.LPWStr)] string pszNewName);
        [PreserveSig] uint PostNewItem(uint dwFlags, IShellItem psiDestinationFolder, [MarshalAs(UnmanagedType.LPWStr)] string pszNewName, [MarshalAs(UnmanagedType.LPWStr)] string pszTemplateName, uint hrNew, IShellItem psiNewlyCreated);
        [PreserveSig] uint UpdateProgress(uint iWorkTotal, uint iWorkSoFar);
        [PreserveSig] uint ResetTimer();
        [PreserveSig] uint PauseTimer();
        [PreserveSig] uint ResumeTimer();
    }

    /// <summary>
    /// Exposes methods that retrieve information about a Shell item.
    /// </summary>
    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("43826d1e-e718-42ee-bc55-a1e261c37bfe")]
    public interface IShellItem
    {
        [PreserveSig] uint BindToHandler(IntPtr pbc, [MarshalAs(UnmanagedType.LPStruct)] Guid bhid, [MarshalAs(UnmanagedType.LPStruct)] Guid riid, out IntPtr ppv);
        [PreserveSig] uint GetParent(out IShellItem ppsi);
        [PreserveSig] uint GetDisplayName(SIGDN sigdnName, [MarshalAs(UnmanagedType.LPWStr)] out string ppszName);
        [PreserveSig] uint GetAttributes(uint sfgaoMask, out uint psfgaoAttribs);
        [PreserveSig] uint Compare(IShellItem psi, uint hint, out int piOrder);
    }

    /// <summary>
    /// Requests the form of an item's display name to retrieve through IShellItem::GetDisplayName.
    /// </summary>
    public enum SIGDN
    {
        NORMALDISPLAY = 0x00000000,
        PARENTRELATIVEPARSING = unchecked((int)0x80018001),
        DESKTOPABSOLUTEPARSING = unchecked((int)0x80028000),
        PARENTRELATIVEEDITING = unchecked((int)0x80031001),
        DESKTOPABSOLUTEEDITING = unchecked((int)0x8004c000),
        FILESYSPATH = unchecked((int)0x80058000),
        URL = unchecked((int)0x80068000),
        PARENTRELATIVEFORADDRESSBAR = unchecked((int)0x8007c001),
        PARENTRELATIVE = unchecked((int)0x80080001),
        PARENTRELATIVEFORUI = unchecked((int)0x80094001)
    }

    /// <summary>
    /// Flags that control file operations performed by IFileOperation.
    /// </summary>
    [Flags]
    [SuppressMessage("Design", "CA2217:Do not mark enums with FlagsAttribute", Justification = "FILEOP_FLAGS is a native Win32 COM Shell flag enum representing operation flags with legacy bitfield values.")]
    public enum FILEOP_FLAGS
    {
        FOF_MULTIDESTFILES = 0x0001,
        FOF_CONFIRMMOUSE = 0x0002,
        FOF_SILENT = 0x0004,
        FOF_RENAMEONCOLLISION = 0x0008,
        FOF_NOCONFIRMATION = 0x0010,
        FOF_WANTMAPPINGHANDLE = 0x0020,
        FOF_ALLOWUNDO = 0x0040,
        FOF_FILESONLY = 0x0080,
        FOF_SIMPLEPROGRESS = 0x0100,
        FOF_NOCONFIRMMKDIR = 0x0200,
        FOF_NOERRORUI = 0x0400,
        FOF_NOCOPYSECURITYATTRIBS = 0x0800,
        FOF_NORECURSION = 0x1000,
        FOF_NO_CONNECTED_ELEMENTS = 0x2000,
        FOF_WANTNUKEWARNING = 0x4000,
        FOF_NORECURSEREPARSE = 0x8000,
        FOFX_NOSKIPWARNINGS = 0x00010000,
        FOFX_PREFERHARDLINK = 0x00020000,
        FOFX_SHOWELEVATIONPROMPT = 0x00040000,
        FOFX_RECYCLEONDELETE = 0x00080000,
        FOFX_EARLYFAILURE = 0x00100000,
        FOFX_PRESERVEFILEEXTENSIONS = 0x00200000,
        FOFX_KEEPNEWERFILE = 0x00400000,
        FOFX_NOCOPYHOOKS = 0x00800000,
        FOFX_NOMINIMIZEBOX = 0x01000000,
        FOFX_MOVEACLSACROSSVOLUMES = 0x02000000,
        FOFX_DONTDISPLAYSOURCEPATH = 0x04000000,
        FOFX_DONTDISPLAYDESTPATH = 0x08000000,
        FOFX_REQUIREELEVATION = 0x10000000,
        FOFX_ADDUNDORECORD = 0x20000000,
        FOFX_COPYASDOWNLOAD = 0x40000000,
        FOFX_DONTDISPLAYLOCATIONS = unchecked((int)0x80000000)
    }
}
