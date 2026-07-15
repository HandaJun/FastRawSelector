using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace FastRawSelector.LOGIC
{
    /// <summary>
    /// 사용자 단위(HKCU) 셸 연동: 보내기(SendTo), 열기 프로그램 목록 (D-2).
    /// 관리자 권한 불필요. 기본 앱 강제 변경은 하지 않는다.
    /// </summary>
    public static class ShellIntegration
    {
        private const string AppExeName = "FastRawSelector.exe";
        private const string SendToLinkName = "FastRawSelector.lnk";

        private static readonly string[] SupportedRawExtensions = new[]
        {
            ".cr2", ".cr3", ".nef", ".nrw", ".arw", ".srf", ".sr2",
            ".raf", ".orf", ".rw2", ".dng", ".pef", ".ptx", ".x3f",
            ".3fr", ".fff", ".mef", ".mos", ".mrw", ".raw", ".rwl"
        };

        public static string SendToShortcutPath
        {
            get
            {
                string sendTo = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Microsoft", "Windows", "SendTo");
                return Path.Combine(sendTo, SendToLinkName);
            }
        }

        public static bool IsSendToRegistered()
        {
            return File.Exists(SendToShortcutPath);
        }

        public static bool IsOpenWithRegistered()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(
                    @"Software\Classes\Applications\" + AppExeName + @"\shell\open\command", false))
                {
                    return key != null && key.GetValue(null) != null;
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>보내기 메뉴에 바로가기 등록/해제.</summary>
        public static void SetSendToRegistered(bool enable)
        {
            string exe = GetExePath();
            if (string.IsNullOrEmpty(exe) || !File.Exists(exe))
            {
                throw new InvalidOperationException("실행 파일 경로를 찾을 수 없습니다.");
            }

            if (enable)
            {
                CreateShortcut(SendToShortcutPath, exe, "FastRawSelector로 열기");
                Log.Info("SendTo 등록: " + SendToShortcutPath);
            }
            else
            {
                if (File.Exists(SendToShortcutPath))
                {
                    File.Delete(SendToShortcutPath);
                    Log.Info("SendTo 해제: " + SendToShortcutPath);
                }
            }
        }

        /// <summary>
        /// 탐색기 "연결 프로그램" 목록에 앱 등록 (HKCU).
        /// 확장자 기본 프로그램 강제 변경은 하지 않음.
        /// </summary>
        public static void SetOpenWithRegistered(bool enable)
        {
            string exe = GetExePath();
            if (string.IsNullOrEmpty(exe) || !File.Exists(exe))
            {
                throw new InvalidOperationException("실행 파일 경로를 찾을 수 없습니다.");
            }

            string appKey = @"Software\Classes\Applications\" + AppExeName;
            if (enable)
            {
                using (var cmd = Registry.CurrentUser.CreateSubKey(appKey + @"\shell\open\command"))
                {
                    if (cmd != null)
                    {
                        cmd.SetValue(null, "\"" + exe + "\" \"%1\"");
                    }
                }
                using (var types = Registry.CurrentUser.CreateSubKey(appKey + @"\SupportedTypes"))
                {
                    if (types != null)
                    {
                        foreach (var ext in SupportedRawExtensions)
                        {
                            types.SetValue(ext, string.Empty);
                        }
                    }
                }
                // Friendly name
                using (var app = Registry.CurrentUser.CreateSubKey(appKey))
                {
                    if (app != null)
                    {
                        app.SetValue("FriendlyAppName", "FastRawSelector");
                    }
                }
                Log.Info("OpenWith 등록(HKCU Applications): " + exe);
            }
            else
            {
                try
                {
                    Registry.CurrentUser.DeleteSubKeyTree(appKey, false);
                    Log.Info("OpenWith 해제: " + appKey);
                }
                catch (Exception ex)
                {
                    Log.Exception(ex);
                }
            }
        }

        private static string GetExePath()
        {
            try
            {
                return System.Reflection.Assembly.GetExecutingAssembly().Location;
            }
            catch
            {
                return Path.Combine(Common.RootPath, AppExeName);
            }
        }

        /// <summary>WScript.Shell COM 으로 .lnk 생성.</summary>
        private static void CreateShortcut(string linkPath, string targetPath, string description)
        {
            string dir = Path.GetDirectoryName(linkPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            Type shellType = Type.GetTypeFromProgID("WScript.Shell");
            if (shellType == null)
            {
                throw new InvalidOperationException("WScript.Shell 을 사용할 수 없습니다.");
            }

            object shell = Activator.CreateInstance(shellType);
            try
            {
                object shortcut = shellType.InvokeMember(
                    "CreateShortcut",
                    System.Reflection.BindingFlags.InvokeMethod,
                    null,
                    shell,
                    new object[] { linkPath });

                Type scType = shortcut.GetType();
                scType.InvokeMember("TargetPath", System.Reflection.BindingFlags.SetProperty, null, shortcut, new object[] { targetPath });
                scType.InvokeMember("WorkingDirectory", System.Reflection.BindingFlags.SetProperty, null, shortcut,
                    new object[] { Path.GetDirectoryName(targetPath) ?? "" });
                scType.InvokeMember("Description", System.Reflection.BindingFlags.SetProperty, null, shortcut, new object[] { description });
                scType.InvokeMember("IconLocation", System.Reflection.BindingFlags.SetProperty, null, shortcut,
                    new object[] { targetPath + ",0" });
                scType.InvokeMember("Save", System.Reflection.BindingFlags.InvokeMethod, null, shortcut, null);
                Marshal.FinalReleaseComObject(shortcut);
            }
            finally
            {
                if (shell != null)
                {
                    Marshal.FinalReleaseComObject(shell);
                }
            }
        }
    }
}
