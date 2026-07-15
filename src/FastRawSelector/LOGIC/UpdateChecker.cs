using System;
using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

namespace FastRawSelector.LOGIC
{
    /// <summary>
    /// GitHub Releases 최신 버전 확인 (Part 7).
    /// 실행 중 exe 교체는 하지 않고, 새 버전이 있으면 Releases 페이지를 연다.
    /// </summary>
    public static class UpdateChecker
    {
        public const string RepoOwner = "HandaJun";
        public const string RepoName = "FastRawSelector";

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

        public class CheckResult
        {
            public bool Ok { get; set; }
            public bool UpdateAvailable { get; set; }
            public string LatestTag { get; set; }
            public Version LatestVersion { get; set; }
            public Version CurrentVersion { get; set; }
            public string HtmlUrl { get; set; }
            public string Error { get; set; }
        }

        /// <summary>현재 어셈블리 버전.</summary>
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

        /// <summary>동기 확인 (백그라운드 스레드에서 호출).</summary>
        public static CheckResult Check()
        {
            var result = new CheckResult
            {
                CurrentVersion = GetCurrentVersion(),
                HtmlUrl = ReleasesPageUrl
            };

            try
            {
                // Windows 7 등에서 GitHub HTTPS
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
                    wc.Encoding = System.Text.Encoding.UTF8;
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

                if (result.LatestVersion == null)
                {
                    result.Error = "bad tag: " + tag;
                    return result;
                }

                // 비교는 Major.Minor.Build 우선 (Revision 무시 가능하도록 Normalize)
                Version cur = Normalize(result.CurrentVersion);
                Version lat = Normalize(result.LatestVersion);
                result.UpdateAvailable = lat > cur;
                result.Ok = true;
                Log.Info(string.Format(
                    "업데이트 확인: current={0}, latest={1} ({2}), available={3}",
                    cur, lat, tag, result.UpdateAvailable));
            }
            catch (Exception ex)
            {
                result.Error = ex.Message;
                Log.Exception(ex);
            }

            return result;
        }

        /// <summary>
        /// 백그라운드 확인 후 UI 콜백.
        /// silentIfUpToDate=true 이면 최신일 때 알림 없음 (기동 시).
        /// </summary>
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
                    OpenUrl(string.IsNullOrEmpty(r.HtmlUrl) ? ReleasesPageUrl : r.HtmlUrl);
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
                try
                {
                    MessageBox.Show(url, Loc.Get("UpdateTitle"), MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch
                {
                }
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
            // 1.0.0-beta 등 접미사 제거
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

        /// <summary>간단 JSON 문자열 필드 추출 (의존성 없이).</summary>
        private static string ExtractJsonString(string json, string key)
        {
            if (string.IsNullOrEmpty(json) || string.IsNullOrEmpty(key))
            {
                return null;
            }
            // "tag_name": "v1.0.0"
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
