using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using Microsoft.Win32;

namespace Easy_Copier.Infrastructure
{
    internal static class NativeShortcutHelper
    {
        private const string AppId = "EasyCopier.App";

        public static void EnsureStartMenuShortcut()
        {
            try
            {
                string exePath = Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty;
                if (string.IsNullOrEmpty(exePath))
                    return;

                string startMenuPath = Environment.GetFolderPath(Environment.SpecialFolder.Programs);
                string shortcutPath = Path.Combine(startMenuPath, "Easy Copier.lnk");

                // If shortcut exists, we could check if it has the correct AUMI. For simplicity, we just recreate it if it doesn't exist,
                // or we can recreate it every time to ensure it's up to date. Let's recreate it to be safe.
                CreateShortcut(shortcutPath, exePath, AppId);

                RegisterAppUserModelId(AppId, "Easy Copier");
            }
            catch (Exception ex)
            {
                // We should not crash the app if shortcut creation fails
                Debug.WriteLine($"Failed to create Start Menu shortcut: {ex.Message}");
            }
        }

                private static void RegisterAppUserModelId(string appId, string displayName)
        {
            try
            {
                string keyPath = $@"Software\Classes\AppUserModelId\{appId}";
                using RegistryKey? key = Registry.CurrentUser.CreateSubKey(keyPath);
                if (key != null)
                {
                    key.SetValue("DisplayName", displayName);

                    string exePath = Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty;
                    if (!string.IsNullOrEmpty(exePath))
                    {
                        key.SetValue("IconUri", exePath);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to register AppUserModelId in registry: {ex.Message}");
            }
        }

        private static void CreateShortcut(string shortcutPath, string exePath, string appId)
        {
            IShellLinkW? link = null;
            IPropertyStore? propertyStore = null;
            IPersistFile? persistFile = null;
            IntPtr propVarValue = IntPtr.Zero;

            try
            {
                link = (IShellLinkW)new ShellLink();
                link.SetPath(exePath);
                link.SetWorkingDirectory(Path.GetDirectoryName(exePath) ?? string.Empty);

                propertyStore = (IPropertyStore)link;
                PropertyKey appUserModelIdKey = new PropertyKey
                {
                    fmtid = Guid.Parse("9F4C2855-9F79-4B39-A8D0-E1D42DE1D5F3"),
                    pid = 5
                };

                PropVariant propVar = new PropVariant();
                propVar.vt = 31; // VT_LPWSTR
                propVarValue = Marshal.StringToCoTaskMemUni(appId);
                propVar.pwszVal = propVarValue;

                propertyStore.SetValue(ref appUserModelIdKey, ref propVar);
                propertyStore.Commit();

                persistFile = (IPersistFile)link;
                persistFile.Save(shortcutPath, true);
            }
            finally
            {
                if (propVarValue != IntPtr.Zero)
                {
                    Marshal.FreeCoTaskMem(propVarValue);
                }
                if (persistFile != null)
                {
                    Marshal.ReleaseComObject(persistFile);
                }
                if (propertyStore != null)
                {
                    Marshal.ReleaseComObject(propertyStore);
                }
                if (link != null)
                {
                    Marshal.ReleaseComObject(link);
                }
            }
        }

        [ComImport]
        [Guid("00021401-0000-0000-C000-000000000046")]
        [ClassInterface(ClassInterfaceType.None)]
        private class ShellLink { }

        [ComImport]
        [Guid("000214F9-0000-0000-C000-000000000046")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IShellLinkW
        {
            void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] string pszFile, int cchMaxPath, IntPtr pfd, uint fFlags);
            void GetIDList(out IntPtr ppidl);
            void SetIDList(IntPtr pidl);
            void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] string pszFile, int cchMaxName);
            void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
            void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] string pszDir, int cchMaxPath);
            void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
            void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] string pszArgs, int cchMaxPath);
            void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
            void GetHotkey(out short pwHotkey);
            void SetHotkey(short wHotkey);
            void GetShowCmd(out uint piShowCmd);
            void SetShowCmd(uint iShowCmd);
            void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int cchIconPath, out int piIcon);
            void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
            void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, uint dwReserved);
            void Resolve(IntPtr hwnd, uint fFlags);
            void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
        }

        [ComImport]
        [Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IPropertyStore
        {
            void GetCount(out uint cProps);
            void GetAt(uint iProp, out PropertyKey pkey);
            void GetValue(ref PropertyKey key, out PropVariant pv);
            void SetValue(ref PropertyKey key, ref PropVariant propvar);
            void Commit();
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PropertyKey
        {
            public Guid fmtid;
            public uint pid;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct PropVariant
        {
            [FieldOffset(0)] public ushort vt;
            [FieldOffset(8)] public IntPtr pwszVal;
        }
    }
}
