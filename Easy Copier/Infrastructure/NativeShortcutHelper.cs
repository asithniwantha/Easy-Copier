using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

namespace Easy_Copier.Infrastructure
{
    public static class NativeShortcutHelper
    {
        [ComImport]
        [Guid("00021401-0000-0000-C000-000000000046")]
        internal class ShellLink
        {
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("000214F9-0000-0000-C000-000000000046")]
        internal interface IShellLinkW
        {
            void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cchMaxPath, out IntPtr pfd, int fFlags);
            void GetIDList(out IntPtr ppidl);
            void SetIDList(IntPtr pidl);
            void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName, int cchMaxName);
            void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
            void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cchMaxPath);
            void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
            void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cchMaxPath);
            void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
            void GetHotkey(out short pwHotkey);
            void SetHotkey(short wHotkey);
            void GetShowCmd(out int piShowCmd);
            void SetShowCmd(int iShowCmd);
            void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath, int cchIconPath, out int piIcon);
            void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
            void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, int dwReserved);
            void Resolve(IntPtr hwnd, int fFlags);
            void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
        }

        [ComImport]
        [Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface IPropertyStore
        {
            void GetCount([Out] out uint cProps);
            void GetAt([In] uint iProp, out PROPERTYKEY pkey);
            void GetValue([In] ref PROPERTYKEY key, [Out] PROPVARIANT pv);
            void SetValue([In] ref PROPERTYKEY key, [In] PROPVARIANT pv);
            void Commit();
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        internal struct PROPERTYKEY
        {
            public Guid fmtid;
            public uint pid;
        }

        [StructLayout(LayoutKind.Explicit)]
        internal struct PROPVARIANT
        {
            [FieldOffset(0)]
            public ushort vt;
            [FieldOffset(8)]
            public IntPtr pwszVal;
        }

        [SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "Only called on Windows")]
        public static void EnsureStartMenuShortcut(string appUserModelId)
        {
            try
            {
                string shortcutName = "Easy Copier.lnk";
                string startMenuPath = Environment.GetFolderPath(Environment.SpecialFolder.Programs);
                string shortcutPath = Path.Combine(startMenuPath, shortcutName);

                string exePath = Environment.ProcessPath ?? string.Empty;
                if (string.IsNullOrEmpty(exePath)) return;

                if (File.Exists(shortcutPath))
                {
                    // For a robust implementation, we would check if it needs updating, but overwriting is safe.
                    File.Delete(shortcutPath);
                }

                IShellLinkW link = (IShellLinkW)new ShellLink();
                link.SetPath(exePath);
                link.SetWorkingDirectory(Path.GetDirectoryName(exePath) ?? string.Empty);

                IPropertyStore propertyStore = (IPropertyStore)link;
                PROPERTYKEY appUserModelIdKey = new()
                {
                    fmtid = new Guid("9F4C2855-9F79-4B39-A8D0-E1D42DE1D5F3"),
                    pid = 5 // PKEY_AppUserModel_ID
                };

                PROPVARIANT propVariant = new();
                propVariant.vt = 31; // VT_LPWSTR
                propVariant.pwszVal = Marshal.StringToCoTaskMemUni(appUserModelId);

                propertyStore.SetValue(ref appUserModelIdKey, propVariant);
                propertyStore.Commit();

                IPersistFile persistFile = (IPersistFile)link;
                persistFile.Save(shortcutPath, true);

                Marshal.FreeCoTaskMem(propVariant.pwszVal);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to create Start Menu shortcut for AUMI: {ex.Message}");
            }
        }
    }
}
