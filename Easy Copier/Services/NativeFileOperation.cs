using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace Easy_Copier.Services
{
    internal static class NativeFileOperation
    {
        [ComImport]
        [Guid("3ad05575-8857-4850-9277-11b85bdb8e09")]
        public class FileOperation { }

        [ComImport]
        [Guid("3ad05575-8857-4850-9277-11b85bdb8e09")]
        [ClassInterface(ClassInterfaceType.None)]
        public class FileOperationClass { }

        [ComImport]
        [Guid("43826d1e-e718-42ee-bc55-a1e261c37bfe")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IShellItem
        {
            void BindToHandler([In] IntPtr pbc, [In] ref Guid bhid, [In] ref Guid riid, out IntPtr ppv);
            void GetParent(out IShellItem ppsi);
            void GetDisplayName([In] uint sigdnName, [MarshalAs(UnmanagedType.LPWStr)] out string ppszName);
            void GetAttributes([In] uint sfgaoMask, out uint psfgaoAttribs);
            void Compare([In] IShellItem psi, [In] uint hint, out int piOrder);
        }

        [ComImport]
        [Guid("947a90f0-1581-4df8-ac22-a9bd9ec68ee4")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [CoClass(typeof(FileOperation))]
        public interface IFileOperation
        {
            uint Advise(IFileOperationProgressSink pfops);
            void Unadvise(uint dwCookie);
            void SetOperationFlags(uint dwOperationFlags);
            void SetProgressMessage([MarshalAs(UnmanagedType.LPWStr)] string pszMessage);
            void SetProgressDialog([In] IntPtr popd);
            void SetProperties([In] IntPtr pproparray);
            void SetOwnerWindow([In] IntPtr hwndOwner);
            void ApplyPropertiesToItem(IShellItem psiItem);
            void ApplyPropertiesToItems([In] IntPtr punkItems);
            void RenameItem(IShellItem psiItem, [MarshalAs(UnmanagedType.LPWStr)] string pszNewName, IFileOperationProgressSink? pfopsItem);
            void RenameItems([In] IntPtr pUnkItems, [MarshalAs(UnmanagedType.LPWStr)] string pszNewName);
            void MoveItem(IShellItem psiItem, IShellItem psiDestinationFolder, [MarshalAs(UnmanagedType.LPWStr)] string? pszNewName, IFileOperationProgressSink? pfopsItem);
            void MoveItems([In] IntPtr punkItems, IShellItem psiDestinationFolder);
            void CopyItem(IShellItem psiItem, IShellItem psiDestinationFolder, [MarshalAs(UnmanagedType.LPWStr)] string? pszCopyName, IFileOperationProgressSink? pfopsItem);
            void CopyItems([In] IntPtr punkItems, IShellItem psiDestinationFolder);
            void DeleteItem(IShellItem psiItem, IFileOperationProgressSink? pfopsItem);
            void DeleteItems([In] IntPtr punkItems);
            void NewItem(IShellItem psiDestinationFolder, uint dwFileAttributes, [MarshalAs(UnmanagedType.LPWStr)] string pszName, [MarshalAs(UnmanagedType.LPWStr)] string pszTemplateName, IFileOperationProgressSink? pfopsItem);
            void PerformOperations();
            [return: MarshalAs(UnmanagedType.Bool)]
            bool GetAnyOperationsAborted();
        }

        [ComImport]
        [Guid("04b0f1a5-8d70-48ea-a08b-6623659f4067")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IFileOperationProgressSink
        {
            void StartOperations();
            void FinishOperations(int hrResult);
            void PreRenameItem(uint dwFlags, IShellItem psiItem, [MarshalAs(UnmanagedType.LPWStr)] string pszNewName);
            void PostRenameItem(uint dwFlags, IShellItem psiItem, [MarshalAs(UnmanagedType.LPWStr)] string pszNewName, int hrRename, IShellItem psiNewlyCreated);
            void PreMoveItem(uint dwFlags, IShellItem psiItem, IShellItem psiDestinationFolder, [MarshalAs(UnmanagedType.LPWStr)] string pszNewName);
            void PostMoveItem(uint dwFlags, IShellItem psiItem, IShellItem psiDestinationFolder, [MarshalAs(UnmanagedType.LPWStr)] string pszNewName, int hrMove, IShellItem psiNewlyCreated);
            void PreCopyItem(uint dwFlags, IShellItem psiItem, IShellItem psiDestinationFolder, [MarshalAs(UnmanagedType.LPWStr)] string pszNewName);
            void PostCopyItem(uint dwFlags, IShellItem psiItem, IShellItem psiDestinationFolder, [MarshalAs(UnmanagedType.LPWStr)] string pszNewName, int hrCopy, IShellItem psiNewlyCreated);
            void PreDeleteItem(uint dwFlags, IShellItem psiItem);
            void PostDeleteItem(uint dwFlags, IShellItem psiItem, int hrDelete, IShellItem psiNewlyCreated);
            void PreNewItem(uint dwFlags, IShellItem psiDestinationFolder, [MarshalAs(UnmanagedType.LPWStr)] string pszNewName);
            void PostNewItem(uint dwFlags, IShellItem psiDestinationFolder, [MarshalAs(UnmanagedType.LPWStr)] string pszNewName, [MarshalAs(UnmanagedType.LPWStr)] string pszTemplateName, uint dwFileAttributes, int hrNew, IShellItem psiNewItem);
            void UpdateProgress(uint iWorkTotal, uint iWorkSoFar);
            void ResetTimer();
            void PauseTimer();
            void ResumeTimer();
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
        public static extern void SHCreateItemFromParsingName(
            [In][MarshalAs(UnmanagedType.LPWStr)] string pszPath,
            [In] IntPtr pbc,
            [In][MarshalAs(UnmanagedType.LPStruct)] Guid riid,
            [Out][MarshalAs(UnmanagedType.Interface)] out IShellItem ppv);

        [DllImport("ole32.dll")]
        public static extern int CoInitializeEx(IntPtr pvReserved, uint dwCoInit);

        [DllImport("ole32.dll")]
        public static extern void CoUninitialize();

        public const uint COINIT_APARTMENTTHREADED = 0x2;

        public static readonly Guid IShellItemGuid = new("43826d1e-e718-42ee-bc55-a1e261c37bfe");

        [Flags]
        public enum FileOperationFlags : uint
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
            FOFX_NOSKIPJUNCTIONS = 0x00010000,
            FOFX_PREFERHARDLINK = 0x00020000,
            FOFX_SHOWELEVATIONPROMPT = 0x00040000,
            FOFX_EARLYFAILURE = 0x00100000,
            FOFX_PRESERVEFILEEXTENSIONS = 0x00200000,
            FOFX_KEEPNEWERFILE = 0x00400000,
            FOFX_NOCOPYHOOKS = 0x00800000,
            FOFX_NOMINIMIZEBOX = 0x01000000,
            FOFX_MOVEACLSACROSSVOLUMES = 0x02000000,
            FOFX_DONTDISPLAYSOURCEPATH = 0x04000000,
            FOFX_DONTDISPLAYDESTPATH = 0x08000000,
            FOFX_RECYCLEONDELETE = 0x00080000,
            FOFX_REQUIREELEVATION = 0x10000000,
            FOFX_COPYASDOWNLOAD = 0x40000000,
            FOFX_DONTDISPLAYLOCATIONS = 0x80000000,
        }
    }
}
