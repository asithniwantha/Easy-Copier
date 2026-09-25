using System;
using System.Runtime.InteropServices;

namespace Easy_Copier.Interop
{
    /// <summary>
    /// Represents the native SIZE structure.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct SIZE
    {
        /// <summary>The x extent (width).</summary>
        public int cx;

        /// <summary>The y extent (height).</summary>
        public int cy;
    }

    /// <summary>
    /// Flags that specify how the image should be generated for IShellItemImageFactory.
    /// </summary>
    [Flags]
    public enum SIIGBF
    {
        /// <summary>Shrink the bitmap as necessary to fit, preserving its aspect ratio.</summary>
        SIIGBF_RESIZETOFIT = 0x00,

        /// <summary>Passed by callers if they want to stretch the returned image themselves. For example, if the caller passes an icon size of 80x80, a 96x96 thumbnail could be returned.</summary>
        SIIGBF_BIGGERSIZEOK = 0x01,

        /// <summary>Return the item only if it is already in memory. Do not access the disk even if the item is cached.</summary>
        SIIGBF_MEMORYONLY = 0x02,

        /// <summary>Return only the icon, never the thumbnail.</summary>
        SIIGBF_ICONONLY = 0x04,

        /// <summary>Return only the thumbnail, never the icon.</summary>
        SIIGBF_THUMBNAILONLY = 0x08,

        /// <summary>Allows access to the disk, but only to retrieve a cached item.</summary>
        SIIGBF_INCACHEONLY = 0x10
    }

    /// <summary>
    /// Exposes a method to return either icons or thumbnails for Shell items.
    /// </summary>
    [ComImport]
    [Guid("bcc18b79-ba16-442f-8a90-8c303536fa9f")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IShellItemImageFactory
    {
        /// <summary>
        /// Gets an HBITMAP that represents an IShellItem.
        /// </summary>
        /// <param name="size">A structure that specifies the size of the image to be received.</param>
        /// <param name="flags">One or more of the SIIGBF flags.</param>
        /// <param name="phbm">Pointer to a value that, when this method returns successfully, receives the handle to the retrieved bitmap.</param>
        /// <returns>HRESULT indicating success or failure.</returns>
        [PreserveSig]
        int GetImage(
            [In] SIZE size,
            [In] SIIGBF flags,
            [Out] out IntPtr phbm);
    }

    /// <summary>
    /// Provides interop helper constants and P/Invoke declarations for GDI32.
    /// </summary>
    public static partial class Gdi32Interop
    {
        /// <summary>
        /// Deletes a logical pen, brush, font, bitmap, region, or palette, freeing all system resources associated with the object.
        /// </summary>
        /// <param name="hObject">A handle to a logical pen, brush, font, bitmap, region, or palette.</param>
        /// <returns>If the function succeeds, the return value is nonzero.</returns>
        [LibraryImport("gdi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool DeleteObject(IntPtr hObject);
    }
}
