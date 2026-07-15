using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

namespace FastRawSelector.LOGIC
{
    /// <summary>
    /// GitHub Releases 확인 → zip/exe 다운로드 → bat 로 실행 중 exe 교체 후 재기동.
    /// (실행 중 자기 자신은 잠기므로, 종료 후 배치가 교체)
    /// </summary>
    public static class UpdateChecker
    {
        public const string RepoOwner = "HandaJun";
        public const string RepoName = "FastRawSelector";
        public const string ExeFileName = "FastRawSelector.exe";

        public static string ReleasesApiUrl
        {
            get
            {
                return "https://api.github.com/repos/" + RepoOwner + "/" + RepoName + "/releases/latest";
            }
        }

        public static string ReleasesPageUrl
        {
            get
            {
                return "https://github.com/" + RepoOwner + "/" + RepoName + "/releases";
            }
        }

        public static string UpdateWorkDir
        {
            get
            {
                return Path.Combine(Common.AppDataPath, "update");
            }
        }

        public class CheckResult
        {
            public bool Ok { get; set; }
            public bool UpdateAvailable { get; set; }
            public string LatestTag { get; set; }
            public Version LatestVersion { get; set; }
            public Version CurrentVersion { get; set; }
            public string HtmlUrl { get; set; }
            /// <summary>GitHub asset 직접 다운로드 URL (.zip 또는 .exe).</summary>
            public string DownloadUrl { get; set; }
            public string Error { get; set; }
        }

        public static Version GetCurrentVersion()
        {
            try
            {
                var v = Assembly.GetExecutingAssembly().GetName().Version;
                return v ?? new Version(0, 0, 0, 0);
            }
            catch
            {
                return new Version(0, 0, 0, 0);
            }
        }

        public static CheckResult Check()
        {
            var result = new CheckResult
            {
                CurrentVersion = GetCurrentVersion(),
                HtmlUrl = ReleasesPageUrl
            };

            try
            {
                try
                {
                    ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
                }
                catch
                {
                }

                string json;
                using (var wc = new WebClient())
                {
                    wc.Encoding = Encoding.UTF8;
                    wc.Headers[HttpRequestHeader.UserAgent] = "FastRawSelector/" + result.CurrentVersion;
                    wc.Headers[HttpRequestHeader.Accept] = "application/vnd.github+json";
                    json = wc.DownloadString(ReleasesApiUrl);
                }

                if (string.IsNullOrEmpty(json))
                {
                    result.Error = "empty response";
                    return result;
                }

                string tag = ExtractJsonString(json, "tag_name");
                string html = ExtractJsonString(json, "html_url");
                if (string.IsNullOrEmpty(tag))
                {
                    result.Error = "no tag_name";
                    return result;
                }

                result.LatestTag = tag;
                result.LatestVersion = ParseVersion(tag);
                if (!string.IsNullOrEmpty(html))
                {
                    result.HtmlUrl = html;
                }
                result.DownloadUrl = PickAssetDownloadUrl(json);

                if (result.LatestVersion == null)
                {
                    result.Error = "bad tag: " + tag;
                    return result;
                }

                Version cur = Normalize(result.CurrentVersion);
                Version lat = Normalize(result.LatestVersion);
                result.UpdateAvailable = lat > cur;
                result.Ok = true;
                Log.Info(string.Format(
                    "업데이트 확인: current={0}, latest={1} ({2}), available={3}, asset={4}",
                    cur, lat, tag, result.UpdateAvailable, result.DownloadUrl ?? "(none)"));
            }
            catch (Exception ex)
            {
                result.Error = ex.Message;
                Log.Exception(ex);
            }

            return result;
        }

        public static void CheckAsync(bool silentIfUpToDate, bool showErrors)
        {
            Task.Run(() =>
            {
                CheckResult r = Check();
                Common.Invoke(() =>
                {
                    try
                    {
                        PresentResult(r, silentIfUpToDate, showErrors);
                    }
                    catch (Exception ex)
                    {
                        Log.Exception(ex);
                    }
                });
            });
        }

        public static void PresentResult(CheckResult r, bool silentIfUpToDate, bool showErrors)
        {
            if (r == null)
            {
                return;
            }

            if (!r.Ok)
            {
                if (showErrors)
                {
                    MessageBox.Show(
                        Loc.GetFormat("UpdateCheckFail", r.Error ?? ""),
                        Loc.Get("UpdateTitle"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
                return;
            }

            if (r.UpdateAvailable)
            {
                string msg = Loc.GetFormat(
                    "UpdateAvailable",
                    r.CurrentVersion != null ? r.CurrentVersion.ToString(3) : "?",
                    r.LatestTag ?? (r.LatestVersion != null ? r.LatestVersion.ToString() : "?"));
                var ans = MessageBox.Show(
                    msg,
                    Loc.Get("UpdateTitle"),
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information);
                if (ans == MessageBoxResult.Yes)
                {
                    if (string.IsNullOrEmpty(r.DownloadUrl))
                    {
                        // asset 없으면 페이지만 열기
                        MessageBox.Show(
                            Loc.Get("UpdateNoAsset"),
                            Loc.Get("UpdateTitle"),
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                        OpenUrl(string.IsNullOrEmpty(r.HtmlUrl) ? ReleasesPageUrl : r.HtmlUrl);
                        return;
                    }
                    StartDownloadAndApply(r);
                }
                return;
            }

            if (!silentIfUpToDate)
            {
                MessageBox.Show(
                    Loc.GetFormat("UpdateLatest", r.CurrentVersion != null ? r.CurrentVersion.ToString(3) : "?"),
                    Loc.Get("UpdateTitle"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }

        /// <summary>다운로드(백그라운드) → bat 교체 → 앱 종료.</summary>
        private static void StartDownloadAndApply(CheckResult r)
        {
            string url = r.DownloadUrl;
            MessageBox.Show(
                Loc.Get("UpdateDownloading"),
                Loc.Get("UpdateTitle"),
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            Task.Run(() =>
            {
                string err = null;
                string newExe = null;
                try
                {
                    newExe = DownloadAndPrepareExe(url);
                    if (string.IsNullOrEmpty(newExe) || !File.Exists(newExe))
                    {
                        err = "no exe after download";
                    }
                }
                catch (Exception ex)
                {
                    err = ex.Message;
                    Log.Exception(ex);
                }

                string exePath = newExe;
                string error = err;
                Common.Invoke(() =>
                {
                    if (!string.IsNullOrEmpty(error) || string.IsNullOrEmpty(exePath))
                    {
                        MessageBox.Show(
                            Loc.GetFormat("UpdateDownloadFail", error ?? ""),
                            Loc.Get("UpdateTitle"),
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                        // 실패 시 수동 페이지
                        OpenUrl(string.IsNullOrEmpty(r.HtmlUrl) ? ReleasesPageUrl : r.HtmlUrl);
                        return;
                    }

                    try
                    {
                        if (!ApplyUpdateWithBatch(exePath))
                        {
                            MessageBox.Show(
                                Loc.Get("UpdateApplyFail"),
                                Loc.Get("UpdateTitle"),
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Exception(ex);
                        MessageBox.Show(
                            Loc.GetFormat("UpdateDownloadFail", ex.Message),
                            Loc.Get("UpdateTitle"),
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                    }
                });
            });
        }

        /// <summary>zip 또는 exe 다운로드 후 교체용 FastRawSelector.exe 경로 반환.</summary>
        public static string DownloadAndPrepareExe(string downloadUrl)
        {
            if (string.IsNullOrEmpty(downloadUrl))
            {
                return null;
            }

            try
            {
                ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            }
            catch
            {
            }

            if (!Directory.Exists(UpdateWorkDir))
            {
                Directory.CreateDirectory(UpdateWorkDir);
            }

            // 이전 작업물 정리
            try
            {
                foreach (var f in Directory.GetFiles(UpdateWorkDir))
                {
                    try { File.Delete(f); } catch { /* ignore */ }
                }
                foreach (var d in Directory.GetDirectories(UpdateWorkDir))
                {
                    try { Directory.Delete(d, true); } catch { /* ignore */ }
                }
            }
            catch
            {
            }

            string fileName = Path.GetFileName(new Uri(downloadUrl).LocalPath);
            if (string.IsNullOrEmpty(fileName))
            {
                fileName = "update.bin";
            }
            string downloadPath = Path.Combine(UpdateWorkDir, fileName);

            using (var wc = new WebClient())
            {
                wc.Headers[HttpRequestHeader.UserAgent] = "FastRawSelector/" + GetCurrentVersion();
                wc.Headers[HttpRequestHeader.Accept] = "application/octet-stream";
                wc.DownloadFile(downloadUrl, downloadPath);
            }

            Log.Info("업데이트 다운로드 완료: " + downloadPath);

            string ext = Path.GetExtension(downloadPath);
            if (string.Equals(ext, ".exe", StringComparison.OrdinalIgnoreCase))
            {
                string dest = Path.Combine(UpdateWorkDir, ExeFileName);
                if (!string.Equals(downloadPath, dest, StringComparison.OrdinalIgnoreCase))
                {
                    File.Copy(downloadPath, dest, true);
                }
                return dest;
            }

            if (string.Equals(ext, ".zip", StringComparison.OrdinalIgnoreCase))
            {
                string extractDir = Path.Combine(UpdateWorkDir, "extracted");
                if (Directory.Exists(extractDir))
                {
                    Directory.Delete(extractDir, true);
                }
                ZipFile.ExtractToDirectory(downloadPath, extractDir);
                string found = FindExeRecursive(extractDir, ExeFileName);
                if (found == null)
                {
                    // 이름이 다르면 첫 exe
                    found = FindFirstExe(extractDir);
                }
                if (found == null)
                {
                    throw new InvalidOperationException("zip has no exe");
                }
                string dest = Path.Combine(UpdateWorkDir, ExeFileName);
                File.Copy(found, dest, true);
                return dest;
            }

            throw new InvalidOperationException("unsupported asset: " + ext);
        }

        /// <summary>
        /// 사용자 샘플과 동일 패턴: bat 생성 → 실행 → 앱 종료.
        /// bat: 대기 → 구 exe 삭제 → 신 exe 복사 → 기동 → bat 삭제.
        /// </summary>
        public static bool ApplyUpdateWithBatch(string newExePath)
        {
            if (string.IsNullOrEmpty(newExePath) || !File.Exists(newExePath))
            {
                return false;
            }

            string exeFileName = GetRunningExePath();
            if (string.IsNullOrEmpty(exeFileName) || !File.Exists(exeFileName))
            {
                Log.Warn("업데이트: 실행 경로를 알 수 없음");
                return false;
            }

            string exeFolder = Path.GetDirectoryName(exeFileName);
            string targetExe = Path.Combine(exeFolder, ExeFileName);
            // 배치 파일은 쓰기 쉬운 AppData 에 둠 (Program Files 대비)
            string batchPath = Path.Combine(UpdateWorkDir, "UpdateFastRawSelector.bat");
            string batchNameOnly = Path.GetFileName(batchPath);

            // 경로 이스케이프 (배치용 따옴표)
            string qNew = QuoteBat(newExePath);
            string qOld = QuoteBat(exeFileName);
            string qTarget = QuoteBat(targetExe);
            string qBat = QuoteBat(batchPath);

            var sb = new StringBuilder();
            sb.AppendLine("@echo off");
            sb.AppendLine("chcp 65001 > nul");
            // 앱 종료·파일 핸들 해제 대기
            sb.AppendLine("ping 127.0.0.1 -n 3 > nul");
            sb.AppendLine(":retry_del");
            sb.AppendLine("del /F /Q " + qOld + " > nul 2>&1");
            sb.AppendLine("if exist " + qOld + " (");
            sb.AppendLine("  ping 127.0.0.1 -n 2 > nul");
            sb.AppendLine("  goto retry_del");
            sb.AppendLine(")");
            // 실행 파일명이 달라도 target 은 FastRawSelector.exe
            sb.AppendLine("copy /Y " + qNew + " " + qTarget + " > nul");
            if (!string.Equals(exeFileName, targetExe, StringComparison.OrdinalIgnoreCase))
            {
                // 예전 이름 exe 로 떠 있던 경우 동일 폴더 복사본 정리 시도
                sb.AppendLine("if not \"" + Path.GetFileName(exeFileName) + "\"==\"" + ExeFileName + "\" del /F /Q " + qOld + " > nul 2>&1");
            }
            sb.AppendLine("start \"\" " + qTarget);
            sb.AppendLine("del /F /Q " + qBat + " > nul 2>&1");

            File.WriteAllText(batchPath, sb.ToString(), new UTF8Encoding(false));
            Log.Info("업데이트 배치 작성: " + batchPath + " -> " + targetExe);

            // 종료 전 설정 저장
            try
            {
                if (App.Setting != null)
                {
                    App.Setting.Save(false);
                }
                if (Common.NowSelectorSetting != null)
                {
                    Common.NowSelectorSetting.Save();
                }
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
            }

            var psi = new ProcessStartInfo
            {
                FileName = batchPath,
                WorkingDirectory = UpdateWorkDir,
                UseShellExecute = true,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };
            Process.Start(psi);

            // 앱 종료 (배치가 교체·재기동)
            Application.Current.Shutdown();
            return true;
        }

        private static string GetRunningExePath()
        {
            try
            {
                string loc = Assembly.GetExecutingAssembly().Location;
                if (!string.IsNullOrEmpty(loc) && File.Exists(loc))
                {
                    return Path.GetFullPath(loc);
                }
            }
            catch
            {
            }
            try
            {
                string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                if (!string.IsNullOrEmpty(codeBase))
                {
                    var uri = new Uri(codeBase);
                    return Path.GetFullPath(uri.LocalPath);
                }
            }
            catch
            {
            }
            return null;
        }

        private static string QuoteBat(string path)
        {
            if (path == null)
            {
                return "\"\"";
            }
            return "\"" + path.Replace("\"", "") + "\"";
        }

        private static string FindExeRecursive(string dir, string name)
        {
            try
            {
                foreach (var f in Directory.GetFiles(dir, name, SearchOption.AllDirectories))
                {
                    return f;
                }
            }
            catch
            {
            }
            return null;
        }

        private static string FindFirstExe(string dir)
        {
            try
            {
                foreach (var f in Directory.GetFiles(dir, "*.exe", SearchOption.AllDirectories))
                {
                    return f;
                }
            }
            catch
            {
            }
            return null;
        }

        /// <summary>release JSON 에서 zip(우선) 또는 exe asset URL.</summary>
        private static string PickAssetDownloadUrl(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return null;
            }
            var matches = Regex.Matches(
                json,
                "\"browser_download_url\"\\s*:\\s*\"([^\"]+)\"",
                RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

            string zipPrefer = null;
            string zipAny = null;
            string exePrefer = null;
            string exeAny = null;

            foreach (Match m in matches)
            {
                if (!m.Success || m.Groups.Count < 2)
                {
                    continue;
                }
                string url = m.Groups[1].Value.Replace("\\u0026", "&");
                string lower = url.ToLowerInvariant();
                bool nameFrs = lower.Contains("fastrawselector");
                if (lower.EndsWith(".zip"))
                {
                    if (nameFrs && zipPrefer == null)
                    {
                        zipPrefer = url;
                    }
                    if (zipAny == null)
                    {
                        zipAny = url;
                    }
                }
                else if (lower.EndsWith(".exe"))
                {
                    if (nameFrs && exePrefer == null)
                    {
                        exePrefer = url;
                    }
                    if (exeAny == null)
                    {
                        exeAny = url;
                    }
                }
            }

            return zipPrefer ?? zipAny ?? exePrefer ?? exeAny;
        }

        public static void OpenUrl(string url)
        {
            try
            {
                if (string.IsNullOrEmpty(url))
                {
                    url = ReleasesPageUrl;
                }
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
            }
        }

        private static Version Normalize(Version v)
        {
            if (v == null)
            {
                return new Version(0, 0, 0, 0);
            }
            int r = v.Revision < 0 ? 0 : v.Revision;
            int b = v.Build < 0 ? 0 : v.Build;
            return new Version(v.Major, v.Minor, b, r);
        }

        public static Version ParseVersion(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
            {
                return null;
            }
            string t = tag.Trim();
            if (t.StartsWith("v", StringComparison.OrdinalIgnoreCase))
            {
                t = t.Substring(1);
            }
            int dash = t.IndexOf('-');
            if (dash > 0)
            {
                t = t.Substring(0, dash);
            }
            Version v;
            if (Version.TryParse(t, out v))
            {
                return v;
            }
            if (Version.TryParse(t + ".0", out v))
            {
                return v;
            }
            return null;
        }

        private static string ExtractJsonString(string json, string key)
        {
            if (string.IsNullOrEmpty(json) || string.IsNullOrEmpty(key))
            {
                return null;
            }
            var m = Regex.Match(
                json,
                "\"" + Regex.Escape(key) + "\"\\s*:\\s*\"([^\"]+)\"",
                RegexOptions.CultureInvariant);
            if (m.Success && m.Groups.Count > 1)
            {
                return m.Groups[1].Value;
            }
            return null;
        }
    }
}
