using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Easy_Copier.Interop
{
    /// <summary>
    /// Provides interop helper constants and P/Invoke declarations for Windows Shell file operations.
    /// </summary>
    public static partial class FileOperationInterop
    {
        /// <summary>
        /// The COM class identifier (CLSID) string for the Windows Shell <see cref="IFileOperation"/> interface.
        /// </summary>
        public const string CLSID_FileOperation = "3AD05575-8857-4850-9277-11B85BDB8E09";

        /// <summary>
        /// Creates and initializes a Shell item object from a parsing name.
        /// </summary>
        [LibraryImport("shell32.dll", EntryPoint = "SHCreateItemFromParsingName", StringMarshalling = StringMarshalling.Utf16)]
        private static partial int SHCreateItemFromParsingNameNative(
            string pszPath,
            IntPtr pbc,
            in Guid riid,
            out IntPtr ppv);

        /// <summary>
        /// Creates and initializes a Shell item object from a parsing name, throwing an exception if the native operation fails.
        /// </summary>
        /// <param name="pszPath">A pointer to a null-terminated display name.</param>
        /// <param name="pbc">A pointer to a bind context that controls the parsing operation.</param>
        /// <param name="riid">A reference to the IID of the requested interface.</param>
        /// <param name="ppv">When this method returns, contains the interface pointer requested in <paramref name="riid"/>.</param>
        internal static void SHCreateItemFromParsingName(
            string pszPath,
            IntPtr pbc,
            Guid riid,
            out IShellItem ppv)
        {
            int hr = SHCreateItemFromParsingNameNative(pszPath, pbc, in riid, out IntPtr ptr);
            if (hr < 0)
            {
                ppv = null!;
                Marshal.ThrowExceptionForHR(hr);
            }
            else
            {
                ppv = (IShellItem)Marshal.GetObjectForIUnknown(ptr);
                _ = Marshal.Release(ptr);
            }
        }
    }

    /// <summary>
    /// Exposes methods for copying, moving, renaming, deleting, and creating Shell items.
    /// </summary>
    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("947aab5f-0a5c-4c13-b4d6-4bf7836fc9f8")]
    public interface IFileOperation
    {
        /// <summary>
        /// Enables a client to receive progress notifications for file operations.
        /// </summary>
        /// <param name="sink">Pointer to an <see cref="IFileOperationProgressSink"/> object.</param>
        /// <param name="pdwCookie">When this method returns, contains a pointer to a token that uniquely identifies this connection.</param>
        /// <returns>An HRESULT success code (0 for S_OK).</returns>
        [PreserveSig] uint Advise(IFileOperationProgressSink sink, out uint pdwCookie);

        /// <summary>
        /// Terminates a notification connection previously established through <see cref="Advise"/>.
        /// </summary>
        /// <param name="dwCookie">The connection token returned by <see cref="Advise"/>.</param>
        /// <returns>An HRESULT success code (0 for S_OK).</returns>
        [PreserveSig] uint Unadvise(uint dwCookie);

        /// <summary>
        /// Sets flags that control file operations.
        /// </summary>
        /// <param name="dwOperationFlags">Flags that control the file operation.</param>
        /// <returns>An HRESULT success code (0 for S_OK).</returns>
        [PreserveSig] uint SetOperationFlags(FILEOP_FLAGS dwOperationFlags);

        /// <summary>
        /// Sets the progress message string.
        /// </summary>
        /// <param name="pszMessage">The progress message text.</param>
        /// <returns>An HRESULT success code (0 for S_OK).</returns>
        [PreserveSig] uint SetProgressMessage([MarshalAs(UnmanagedType.LPWStr)] string pszMessage);

        /// <summary>
        /// Specifies the progress dialog box used by the file operation engine.
        /// </summary>
        /// <param name="popd">Pointer to an IProgressDialog object.</param>
        /// <returns>An HRESULT success code (0 for S_OK).</returns>
        [PreserveSig] uint SetProgressDialog([In] IntPtr popd);

        /// <summary>
        /// Specifies a set of property changes to apply to items.
        /// </summary>
        /// <param name="pproparray">Pointer to an IPropertyChangeArray object.</param>
        /// <returns>An HRESULT success code (0 for S_OK).</returns>
        [PreserveSig] uint SetProperties([In] IntPtr pproparray);

        /// <summary>
        /// Sets the parent or owner window for UI dialogs displayed during operations.
        /// </summary>
        /// <param name="hwndOwner">The parent window handle.</param>
        /// <returns>An HRESULT success code (0 for S_OK).</returns>
        [PreserveSig] uint SetOwnerWindow(IntPtr hwndOwner);

        /// <summary>
        /// Declares a single item whose properties are to be set.
        /// </summary>
        /// <param name="psiItem">Pointer to the target <see cref="IShellItem"/>.</param>
        /// <returns>An HRESULT success code (0 for S_OK).</returns>
        [PreserveSig] uint ApplyPropertiesToItem(IShellItem psiItem);

        /// <summary>
        /// Declares a set of items whose properties are to be set.
        /// </summary>
        /// <param name="punkItems">Pointer to an IShellItemArray object containing target items.</param>
        /// <returns>An HRESULT success code (0 for S_OK).</returns>
        [PreserveSig] uint ApplyPropertiesToItems([In] IntPtr punkItems);

        /// <summary>
        /// Declares a single item that is to be renamed.
        /// </summary>
        /// <param name="psiItem">Pointer to the target <see cref="IShellItem"/>.</param>
        /// <param name="pszNewName">The new display name of the item.</param>
        /// <param name="pfopsItem">Pointer to an <see cref="IFileOperationProgressSink"/> object for operation notifications.</param>
        /// <returns>An HRESULT success code (0 for S_OK).</returns>
        [PreserveSig] uint RenameItem(IShellItem psiItem, [MarshalAs(UnmanagedType.LPWStr)] string pszNewName, IFileOperationProgressSink? pfopsItem);

        /// <summary>
        /// Declares a set of items that are to be given a new display name.
        /// </summary>
        /// <param name="pUnkItems">Pointer to an IShellItemArray object containing target items.</param>
        /// <param name="pszNewName">The new display name to apply.</param>
        /// <returns>An HRESULT success code (0 for S_OK).</returns>
        [PreserveSig] uint RenameItems([In] IntPtr pUnkItems, [MarshalAs(UnmanagedType.LPWStr)] string pszNewName);

        /// <summary>
        /// Declares a single item that is to be moved to a specified destination.
        /// </summary>
        /// <param name="psiItem">Pointer to the source <see cref="IShellItem"/>.</param>
        /// <param name="psiDestinationFolder">Pointer to the destination folder <see cref="IShellItem"/>.</param>
        /// <param name="pszNewName">An optional new name for the item at the destination.</param>
        /// <param name="pfopsItem">Pointer to an <see cref="IFileOperationProgressSink"/> object for operation notifications.</param>
        /// <returns>An HRESULT success code (0 for S_OK).</returns>
        [PreserveSig] uint MoveItem(IShellItem psiItem, IShellItem psiDestinationFolder, [MarshalAs(UnmanagedType.LPWStr)] string? pszNewName, IFileOperationProgressSink? pfopsItem);

        /// <summary>
        /// Declares a set of items that are to be moved to a specified destination.
        /// </summary>
        /// <param name="punkItems">Pointer to an IShellItemArray object containing items to move.</param>
        /// <param name="psiDestinationFolder">Pointer to the destination folder <see cref="IShellItem"/>.</param>
        /// <returns>An HRESULT success code (0 for S_OK).</returns>
        [PreserveSig] uint MoveItems([In] IntPtr punkItems, IShellItem psiDestinationFolder);

        /// <summary>
        /// Declares a single item that is to be copied to a specified destination.
        /// </summary>
        /// <param name="psiItem">Pointer to the source <see cref="IShellItem"/>.</param>
        /// <param name="psiDestinationFolder">Pointer to the destination folder <see cref="IShellItem"/>.</param>
        /// <param name="pszCopyName">An optional new name for the item at the destination.</param>
        /// <param name="pfopsItem">Pointer to an <see cref="IFileOperationProgressSink"/> object for operation notifications.</param>
        /// <returns>An HRESULT success code (0 for S_OK).</returns>
        [PreserveSig] uint CopyItem(IShellItem psiItem, IShellItem psiDestinationFolder, [MarshalAs(UnmanagedType.LPWStr)] string? pszCopyName, IFileOperationProgressSink? pfopsItem);

        /// <summary>
        /// Declares a set of items that are to be copied to a specified destination.
        /// </summary>
        /// <param name="punkItems">Pointer to an IShellItemArray object containing items to copy.</param>
        /// <param name="psiDestinationFolder">Pointer to the destination folder <see cref="IShellItem"/>.</param>
        /// <returns>An HRESULT success code (0 for S_OK).</returns>
        [PreserveSig] uint CopyItems([In] IntPtr punkItems, IShellItem psiDestinationFolder);

        /// <summary>
        /// Declares a single item that is to be deleted.
        /// </summary>
        /// <param name="psiItem">Pointer to the target <see cref="IShellItem"/> to delete.</param>
        /// <param name="pfopsItem">Pointer to an <see cref="IFileOperationProgressSink"/> object for operation notifications.</param>
        /// <returns>An HRESULT success code (0 for S_OK).</returns>
        [PreserveSig] uint DeleteItem(IShellItem psiItem, IFileOperationProgressSink? pfopsItem);

        /// <summary>
        /// Declares a set of items that are to be deleted.
        /// </summary>
        /// <param name="punkItems">Pointer to an IShellItemArray object containing items to delete.</param>
        /// <returns>An HRESULT success code (0 for S_OK).</returns>
        [PreserveSig] uint DeleteItems([In] IntPtr punkItems);

        /// <summary>
        /// Declares a new item that is to be created in a specified location.
        /// </summary>
        /// <param name="psiDestinationFolder">Pointer to the parent destination folder <see cref="IShellItem"/>.</param>
        /// <param name="dwFileAttributes">File attribute flags for the new item.</param>
        /// <param name="pszName">The file name of the new item.</param>
        /// <param name="pszTemplateName">Optional template file name to base the new item on.</param>
        /// <param name="pfopsItem">Pointer to an <see cref="IFileOperationProgressSink"/> object for operation notifications.</param>
        /// <returns>An HRESULT success code (0 for S_OK).</returns>
        [PreserveSig] uint NewItem(IShellItem psiDestinationFolder, [In] IntPtr dwFileAttributes, [MarshalAs(UnmanagedType.LPWStr)] string pszName, [MarshalAs(UnmanagedType.LPWStr)] string? pszTemplateName, IFileOperationProgressSink? pfopsItem);

        /// <summary>
        /// Executes all configured file operations.
        /// </summary>
        /// <returns>An HRESULT success code (0 for S_OK).</returns>
        [PreserveSig] uint PerformOperations();

        /// <summary>
        /// Gets a value indicating whether any file operations declared by this instance were aborted prior to completion.
        /// </summary>
        /// <param name="pfAnyOperationsAborted">When this method returns, contains <see langword="true"/> if any operations were aborted; otherwise, <see langword="false"/>.</param>
        /// <returns>An HRESULT success code (0 for S_OK).</returns>
        [PreserveSig] uint GetAnyOperationsAborted([MarshalAs(UnmanagedType.Bool)] out bool pfAnyOperationsAborted);
    }

    /// <summary>
    /// Exposes status and notification methods for file operation events.
    /// </summary>
    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("04b0f1a5-8d70-48ea-a083-d161a65492d4")]
    public interface IFileOperationProgressSink
    {
        /// <summary>
        /// Performs caller-defined actions before file operations begin.
        /// </summary>
        /// <returns>An HRESULT success code (0 for S_OK).</returns>
        [PreserveSig] uint StartOperations();

        /// <summary>
        /// Performs caller-defined actions after all file operations have completed.
        /// </summary>
        /// <param name="hrResult">The final result code of the overall operation.</param>
        /// <returns>An HRESULT success code (0 for S_OK).</returns>
        [PreserveSig] uint FinishOperations(uint hrResult);

        /// <summary>
        /// Performs caller-defined actions before the rename operation for an item begins.
        /// </summary>
        /// <param name="dwFlags">Operation control flags.</param>
        /// <param name="psiItem">Pointer to the target <see cref="IShellItem"/>.</param>
        /// <param name="pszNewName">The proposed new name.</param>
        /// <returns>An HRESULT success code (0 for S_OK).</returns>
        [PreserveSig] uint PreRenameItem(uint dwFlags, IShellItem psiItem, [MarshalAs(UnmanagedType.LPWStr)] string pszNewName);

        /// <summary>
        /// Performs caller-defined actions after the rename operation for an item completes.
        /// </summary>
        /// <param name="dwFlags">Operation control flags.</param>
        /// <param name="psiItem">Pointer to the source <see cref="IShellItem"/>.</param>
        /// <param name="pszNewName">The new display name.</param>
        /// <param name="hrRename">The result of the rename operation.</param>
        /// <param name="psiNewlyCreated">Pointer to the renamed <see cref="IShellItem"/>.</param>
        /// <returns>An HRESULT success code (0 for S_OK).</returns>
        [PreserveSig] uint PostRenameItem(uint dwFlags, IShellItem psiItem, [MarshalAs(UnmanagedType.LPWStr)] string pszNewName, uint hrRename, IShellItem psiNewlyCreated);

        /// <summary>
        /// Performs caller-defined actions before the move operation for an item begins.
        /// </summary>
        /// <param name="dwFlags">Operation control flags.</param>
        /// <param name="psiItem">Pointer to the source <see cref="IShellItem"/>.</param>
        /// <param name="psiDestinationFolder">Pointer to the destination folder <see cref="IShellItem"/>.</param>
        /// <param name="pszNewName">The proposed new name.</param>
        /// <returns>An HRESULT success code (0 for S_OK).</returns>
        [PreserveSig] uint PreMoveItem(uint dwFlags, IShellItem psiItem, IShellItem psiDestinationFolder, [MarshalAs(UnmanagedType.LPWStr)] string pszNewName);

        /// <summary>
        /// Performs caller-defined actions after the move operation for an item completes.
        /// </summary>
        /// <param name="dwFlags">Operation control flags.</param>
        /// <param name="psiItem">Pointer to the source <see cref="IShellItem"/>.</param>
        /// <param name="psiDestinationFolder">Pointer to the destination folder <see cref="IShellItem"/>.</param>
        /// <param name="pszNewName">The target item name.</param>
        /// <param name="hrMove">The result of the move operation.</param>
        /// <param name="psiNewlyCreated">Pointer to the moved <see cref="IShellItem"/>.</param>
        /// <returns>An HRESULT success code (0 for S_OK).</returns>
        [PreserveSig] uint PostMoveItem(uint dwFlags, IShellItem psiItem, IShellItem psiDestinationFolder, [MarshalAs(UnmanagedType.LPWStr)] string pszNewName, uint hrMove, IShellItem psiNewlyCreated);

        /// <summary>
        /// Performs caller-defined actions before the copy operation for an item begins.
        /// </summary>
        /// <param name="dwFlags">Operation control flags.</param>
        /// <param name="psiItem">Pointer to the source <see cref="IShellItem"/>.</param>
        /// <param name="psiDestinationFolder">Pointer to the destination folder <see cref="IShellItem"/>.</param>
        /// <param name="pszNewName">The proposed destination name.</param>
        /// <returns>An HRESULT success code (0 for S_OK).</returns>
        [PreserveSig] uint PreCopyItem(uint dwFlags, IShellItem psiItem, IShellItem psiDestinationFolder, [MarshalAs(UnmanagedType.LPWStr)] string pszNewName);

        /// <summary>
        /// Performs caller-defined actions after the copy operation for an item completes.
        /// </summary>
        /// <param name="dwFlags">Operation control flags.</param>
        /// <param name="psiItem">Pointer to the source <see cref="IShellItem"/>.</param>
        /// <param name="psiDestinationFolder">Pointer to the destination folder <see cref="IShellItem"/>.</param>
        /// <param name="pszNewName">The destination item name.</param>
        /// <param name="hrCopy">The result of the copy operation.</param>
        /// <param name="psiNewlyCreated">Pointer to the newly copied <see cref="IShellItem"/>.</param>
        /// <returns>An HRESULT success code (0 for S_OK).</returns>
        [PreserveSig] uint PostCopyItem(uint dwFlags, IShellItem psiItem, IShellItem psiDestinationFolder, [MarshalAs(UnmanagedType.LPWStr)] string pszNewName, uint hrCopy, IShellItem psiNewlyCreated);

        /// <summary>
        /// Performs caller-defined actions before the delete operation for an item begins.
        /// </summary>
        /// <param name="dwFlags">Operation control flags.</param>
        /// <param name="psiItem">Pointer to the target <see cref="IShellItem"/>.</param>
        /// <returns>An HRESULT success code (0 for S_OK).</returns>
        [PreserveSig] uint PreDeleteItem(uint dwFlags, IShellItem psiItem);

        /// <summary>
        /// Performs caller-defined actions after the delete operation for an item completes.
        /// </summary>
        /// <param name="dwFlags">Operation control flags.</param>
        /// <param name="psiItem">Pointer to the deleted <see cref="IShellItem"/>.</param>
        /// <param name="hrDelete">The result of the delete operation.</param>
        /// <param name="psiNewlyCreated">Pointer to any newly created item, such as a item in the Recycle Bin.</param>
        /// <returns>An HRESULT success code (0 for S_OK).</returns>
        [PreserveSig] uint PostDeleteItem(uint dwFlags, IShellItem psiItem, uint hrDelete, IShellItem psiNewlyCreated);

        /// <summary>
        /// Performs caller-defined actions before creating a new item.
        /// </summary>
        /// <param name="dwFlags">Operation control flags.</param>
        /// <param name="psiDestinationFolder">Pointer to the destination folder <see cref="IShellItem"/>.</param>
        /// <param name="pszNewName">The proposed name for the new item.</param>
        /// <returns>An HRESULT success code (0 for S_OK).</returns>
        [PreserveSig] uint PreNewItem(uint dwFlags, IShellItem psiDestinationFolder, [MarshalAs(UnmanagedType.LPWStr)] string pszNewName);

        /// <summary>
        /// Performs caller-defined actions after a new item is created.
        /// </summary>
        /// <param name="dwFlags">Operation control flags.</param>
        /// <param name="psiDestinationFolder">Pointer to the parent folder <see cref="IShellItem"/>.</param>
        /// <param name="pszNewName">The name of the new item.</param>
        /// <param name="pszTemplateName">The template name used to create the item.</param>
        /// <param name="hrNew">The result of the creation operation.</param>
        /// <param name="psiNewlyCreated">Pointer to the newly created <see cref="IShellItem"/>.</param>
        /// <returns>An HRESULT success code (0 for S_OK).</returns>
        [PreserveSig] uint PostNewItem(uint dwFlags, IShellItem psiDestinationFolder, [MarshalAs(UnmanagedType.LPWStr)] string pszNewName, [MarshalAs(UnmanagedType.LPWStr)] string pszTemplateName, uint hrNew, IShellItem psiNewlyCreated);

        /// <summary>
        /// Provides progress measurement feedback for ongoing file operations.
        /// </summary>
        /// <param name="iWorkTotal">The total amount of work to be performed.</param>
        /// <param name="iWorkSoFar">The amount of work completed so far.</param>
        /// <returns>An HRESULT success code (0 for S_OK).</returns>
        [PreserveSig] uint UpdateProgress(uint iWorkTotal, uint iWorkSoFar);

        /// <summary>
        /// Resets the progress timer.
        /// </summary>
        /// <returns>An HRESULT success code (0 for S_OK).</returns>
        [PreserveSig] uint ResetTimer();

        /// <summary>
        /// Pauses the progress timer.
        /// </summary>
        /// <returns>An HRESULT success code (0 for S_OK).</returns>
        [PreserveSig] uint PauseTimer();

        /// <summary>
        /// Resumes the progress timer.
        /// </summary>
        /// <returns>An HRESULT success code (0 for S_OK).</returns>
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
        /// <summary>
        /// Binds to a handler for an item as specified by the handler ID value.
        /// </summary>
        /// <param name="pbc">Pointer to an IBindCtx interface on a bind context object.</param>
        /// <param name="bhid">Reference to the GUID specifying the handler handler type.</param>
        /// <param name="riid">Reference to the IID of the interface to return through <paramref name="ppv"/>.</param>
        /// <param name="ppv">When this method returns, contains the interface pointer requested in <paramref name="riid"/>.</param>
        /// <returns>An HRESULT success code (0 for S_OK).</returns>
        [PreserveSig] uint BindToHandler(IntPtr pbc, [MarshalAs(UnmanagedType.LPStruct)] Guid bhid, [MarshalAs(UnmanagedType.LPStruct)] Guid riid, out IntPtr ppv);

        /// <summary>
        /// Gets the parent of an <see cref="IShellItem"/> object.
        /// </summary>
        /// <param name="ppsi">When this method returns, contains the parent <see cref="IShellItem"/> pointer.</param>
        /// <returns>An HRESULT success code (0 for S_OK).</returns>
        [PreserveSig] uint GetParent(out IShellItem ppsi);

        /// <summary>
        /// Gets the display name of the <see cref="IShellItem"/> object.
        /// </summary>
        /// <param name="sigdnName">One of the <see cref="SIGDN"/> values that indicates how the name should look.</param>
        /// <param name="ppszName">When this method returns, contains the retrieved display name string.</param>
        /// <returns>An HRESULT success code (0 for S_OK).</returns>
        [PreserveSig] uint GetDisplayName(SIGDN sigdnName, [MarshalAs(UnmanagedType.LPWStr)] out string ppszName);

        /// <summary>
        /// Gets a requested set of attributes of the <see cref="IShellItem"/> object.
        /// </summary>
        /// <param name="sfgaoMask">Mask of attributes to request.</param>
        /// <param name="psfgaoAttribs">When this method returns, contains the requested attribute flags.</param>
        /// <returns>An HRESULT success code (0 for S_OK).</returns>
        [PreserveSig] uint GetAttributes(uint sfgaoMask, out uint psfgaoAttribs);

        /// <summary>
        /// Compares two <see cref="IShellItem"/> objects.
        /// </summary>
        /// <param name="psi">Pointer to an <see cref="IShellItem"/> object to compare with the current item.</param>
        /// <param name="hint">Flags that determine how the comparison is made.</param>
        /// <param name="piOrder">When this method returns, contains less than 0 if this item comes before <paramref name="psi"/>, 0 if equal, or greater than 0 if after.</param>
        /// <returns>An HRESULT success code (0 for S_OK).</returns>
        [PreserveSig] uint Compare(IShellItem psi, uint hint, out int piOrder);
    }

    /// <summary>
    /// Requests the form of an item's display name to retrieve through <see cref="IShellItem.GetDisplayName"/>.
    /// </summary>
    public enum SIGDN
    {
        /// <summary>Returns the display name relative to the parent folder.</summary>
        NORMALDISPLAY = 0x00000000,

        /// <summary>Returns the parsing name relative to the parent folder.</summary>
        PARENTRELATIVEPARSING = unchecked((int)0x80018001),

        /// <summary>Returns the path relative to the desktop.</summary>
        DESKTOPABSOLUTEPARSING = unchecked((int)0x80028000),

        /// <summary>Returns the editing name relative to the parent folder.</summary>
        PARENTRELATIVEEDITING = unchecked((int)0x80031001),

        /// <summary>Returns the editing name relative to the desktop.</summary>
        DESKTOPABSOLUTEEDITING = unchecked((int)0x8004c000),

        /// <summary>Returns the item's file system path, if available.</summary>
        FILESYSPATH = unchecked((int)0x80058000),

        /// <summary>Returns the item's URL path, if available.</summary>
        URL = unchecked((int)0x80068000),

        /// <summary>Returns the path relative to the parent folder suitable for display in an address bar.</summary>
        PARENTRELATIVEFORADDRESSBAR = unchecked((int)0x8007c001),

        /// <summary>Returns the name relative to the parent folder.</summary>
        PARENTRELATIVE = unchecked((int)0x80080001),

        /// <summary>Returns the name relative to the parent folder suitable for display in UI.</summary>
        PARENTRELATIVEFORUI = unchecked((int)0x80094001)
    }

    /// <summary>
    /// Flags that control file operations performed by <see cref="IFileOperation"/>.
    /// </summary>
    [Flags]
    [SuppressMessage("Design", "CA1028:Enum Storage should be Int32", Justification = "Windows IFileOperation flags map to an unsigned DWORD, including the 0x80000000 bit.")]
    public enum FILEOP_FLAGS : uint
    {
        /// <summary>Assigns multiple destination files for each source file.</summary>
        FOF_MULTIDESTFILES = 0x0001,

        /// <summary>Not used.</summary>
        FOF_CONFIRMMOUSE = 0x0002,

        /// <summary>Does not display a progress dialog box.</summary>
        FOF_SILENT = 0x0004,

        /// <summary>Gives the item being operated on a new name in collision operations.</summary>
        FOF_RENAMEONCOLLISION = 0x0008,

        /// <summary>Responds with 'Yes to All' for any dialog box presented.</summary>
        FOF_NOCONFIRMATION = 0x0010,

        /// <summary>Preserves undo information if possible.</summary>
        FOF_WANTMAPPINGHANDLE = 0x0020,

        /// <summary>Preserves undo information, if possible.</summary>
        FOF_ALLOWUNDO = 0x0040,

        /// <summary>Performs the operation on files only if a wildcard file name is specified.</summary>
        FOF_FILESONLY = 0x0080,

        /// <summary>Displays a progress dialog box but does not show file names.</summary>
        FOF_SIMPLEPROGRESS = 0x0100,

        /// <summary>Does not confirm creation of a new directory if the operation requires one to be created.</summary>
        FOF_NOCONFIRMMKDIR = 0x0200,

        /// <summary>Does not display a user interface if an error occurs.</summary>
        FOF_NOERRORUI = 0x0400,

        /// <summary>Does not copy security attributes of the file.</summary>
        FOF_NOCOPYSECURITYATTRIBS = 0x0800,

        /// <summary>Only operates in the local directory. Does not operate recursively into subdirectories.</summary>
        FOF_NORECURSION = 0x1000,

        /// <summary>Does not move connected items as a group.</summary>
        FOF_NO_CONNECTED_ELEMENTS = 0x2000,

        /// <summary>Warns if an item is being permanently destroyed during delete.</summary>
        FOF_WANTNUKEWARNING = 0x4000,

        /// <summary>Does not traverse reparse points.</summary>
        FOF_NORECURSEREPARSE = 0x8000,

        /// <summary>Does not skip warnings.</summary>
        FOFX_NOSKIPWARNINGS = 0x00010000,

        /// <summary>Prefers creation of a hard link over copying.</summary>
        FOFX_PREFERHARDLINK = 0x00020000,

        /// <summary>Shows elevation prompt if required.</summary>
        FOFX_SHOWELEVATIONPROMPT = 0x00040000,

        /// <summary>Recycles items on delete if possible.</summary>
        FOFX_RECYCLEONDELETE = 0x00080000,

        /// <summary>Fails immediately if any single operation fails.</summary>
        FOFX_EARLYFAILURE = 0x00100000,

        /// <summary>Preserves file extensions during rename operations.</summary>
        FOFX_PRESERVEFILEEXTENSIONS = 0x00200000,

        /// <summary>Keeps the newer file during collisions.</summary>
        FOFX_KEEPNEWERFILE = 0x00400000,

        /// <summary>Does not execute copy hooks.</summary>
        FOFX_NOCOPYHOOKS = 0x00800000,

        /// <summary>Does not display a minimize box on progress dialogs.</summary>
        FOFX_NOMINIMIZEBOX = 0x01000000,

        /// <summary>Moves access control lists across volumes.</summary>
        FOFX_MOVEACLSACROSSVOLUMES = 0x02000000,

        /// <summary>Does not display source path in progress UI.</summary>
        FOFX_DONTDISPLAYSOURCEPATH = 0x04000000,

        /// <summary>Does not display destination path in progress UI.</summary>
        FOFX_DONTDISPLAYDESTPATH = 0x08000000,

        /// <summary>Requires UAC elevation for the operation.</summary>
        FOFX_REQUIREELEVATION = 0x10000000,

        /// <summary>Adds an undo record.</summary>
        FOFX_ADDUNDORECORD = 0x20000000,

        /// <summary>Treats copy operation as a download.</summary>
        FOFX_COPYASDOWNLOAD = 0x40000000,

        /// <summary>Does not display source or destination locations in progress UI.</summary>
        FOFX_DONTDISPLAYLOCATIONS = 0x80000000
    }
}
