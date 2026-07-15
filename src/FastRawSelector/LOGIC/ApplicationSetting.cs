using System;
using System.IO;
using System.Windows.Input;
using YamlDotNet.Serialization;

namespace FastRawSelector.LOGIC
{
    [Serializable]
    public class ApplicationSetting
    {
        /// <summary>%AppData%\Roaming\FastRawSelector\FastRawSelectorSetting.yaml</summary>
        public static string Location
        {
            get
            {
                return Path.Combine(Common.AppDataPath, "FastRawSelectorSetting.yaml");
            }
        }

        public bool AllowNotRawImage { get; set; } = false;
        public bool IsExifVisible { get; set; } = false;

        /// <summary>
        /// 로그 루트 레벨. "INFO"(기본) 또는 "DEBUG".
        /// </summary>
        public string LogLevel { get; set; } = "INFO";

        /// <summary>메인 창을 항상 위에 표시 (Topmost).</summary>
        public bool AlwaysOnTop { get; set; } = false;

        /// <summary>테마. "Dark"(기본) 또는 "Light".</summary>
        public string Theme { get; set; } = "Dark";

        /// <summary>UI 언어. "ko"(기본) / "ja" / "en".</summary>
        public string Language { get; set; } = "ko";

        /// <summary>마지막으로 연 사진 폴더 경로.</summary>
        public string LastFolderPath { get; set; } = "";

        /// <summary>기동 시 LastFolderPath 자동 열기.</summary>
        public bool OpenLastFolderOnStartup { get; set; } = false;

        /// <summary>기동 시 GitHub Releases 로 업데이트 확인 (기본 켜짐).</summary>
        public bool AutoCheckUpdate { get; set; } = true;

        /// <summary>선택 토글 단축키 (단일 키, 기본 B).</summary>
        public string KeySelect { get; set; } = "B";

        /// <summary>EXIF 표시 토글 단축키 (기본 I).</summary>
        public string KeyExif { get; set; } = "I";

        /// <summary>전체화면 단축키 (기본 F).</summary>
        public string KeyFullScreen { get; set; } = "F";

        /// <summary>그리드 썸네일 한 변 크기(px). 100~1200.</summary>
        public int GridItemSize { get; set; } = 160;

        /// <summary>그리드 좌측 폴더 패널 너비(px).</summary>
        public double GridPaneWidth { get; set; } = 200;

        /// <summary>메인 창 Left. null 이면 미복원 (Part 6).</summary>
        public double? WindowLeft { get; set; }

        /// <summary>메인 창 Top.</summary>
        public double? WindowTop { get; set; }

        /// <summary>메인 창 Width. null/0 이하면 미복원.</summary>
        public double? WindowWidth { get; set; }

        /// <summary>메인 창 Height. null/0 이하면 미복원.</summary>
        public double? WindowHeight { get; set; }

        /// <summary>창 상태. "Normal" / "Maximized" (Minimized 는 Normal 로 저장).</summary>
        public string WindowState { get; set; } = "Normal";

        public static ApplicationSetting Load()
        {
            if (!Directory.Exists(Common.AppDataPath))
            {
                Directory.CreateDirectory(Common.AppDataPath);
            }

            string legacyPath = Path.Combine(Common.RootPath, "FastRawSelectorSetting.yaml");
            if (!File.Exists(Location) && File.Exists(legacyPath))
            {
                try
                {
                    File.Copy(legacyPath, Location, false);
                    try { File.Delete(legacyPath); } catch { /* ignore */ }
                }
                catch (Exception ex)
                {
                    Log.Exception(ex);
                }
            }

            if (!File.Exists(Location))
            {
                using (var fs = File.Create(Location)) { }
            }

            ApplicationSetting setting = null;
            try
            {
                setting = Deserialize(Location);
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
            }

            if (setting == null)
            {
                setting = new ApplicationSetting();
                setting.Save(false);
            }

            setting.NormalizeKeys();
            return setting;
        }

        public void Save(bool tabDataClearFlg = true)
        {
            if (!Directory.Exists(Common.AppDataPath))
            {
                Directory.CreateDirectory(Common.AppDataPath);
            }
            NormalizeKeys();
            Serialize(this, Location);
        }

        /// <summary>단축키 문자열을 유효한 Key 로 정규화.</summary>
        public void NormalizeKeys()
        {
            KeySelect = NormalizeKeyName(KeySelect, "B");
            KeyExif = NormalizeKeyName(KeyExif, "I");
            KeyFullScreen = NormalizeKeyName(KeyFullScreen, "F");
            if (string.IsNullOrWhiteSpace(Theme) ||
                (Theme != "Dark" && Theme != "Light"))
            {
                Theme = "Dark";
            }
            Language = Loc.Normalize(Language);
            if (string.IsNullOrWhiteSpace(LogLevel))
            {
                LogLevel = "INFO";
            }
            if (GridItemSize < 100)
            {
                GridItemSize = 100;
            }
            else if (GridItemSize > 1200)
            {
                GridItemSize = 1200;
            }
            if (GridPaneWidth < 120)
            {
                GridPaneWidth = 120;
            }
            else if (GridPaneWidth > 480)
            {
                GridPaneWidth = 480;
            }
            if (string.IsNullOrWhiteSpace(WindowState)
                || (WindowState != "Normal" && WindowState != "Maximized"))
            {
                WindowState = "Normal";
            }
        }

        public Key GetSelectKey()
        {
            return ParseKey(KeySelect, Key.B);
        }

        public Key GetExifKey()
        {
            return ParseKey(KeyExif, Key.I);
        }

        public Key GetFullScreenKey()
        {
            return ParseKey(KeyFullScreen, Key.F);
        }

        public static Key ParseKey(string name, Key fallback)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return fallback;
            }
            Key k;
            if (Enum.TryParse(name.Trim(), true, out k) && k != Key.None)
            {
                return k;
            }
            return fallback;
        }

        private static string NormalizeKeyName(string name, string fallback)
        {
            Key k = ParseKey(name, ParseKey(fallback, Key.B));
            return k.ToString();
        }

        private static void Serialize(ApplicationSetting setting, string path)
        {
            var serializer = new SerializerBuilder().Build();
            var yml = serializer.Serialize(setting);
            using (var sr = new StreamWriter(path))
            {
                sr.Write(yml);
            }
        }

        private static ApplicationSetting Deserialize(string path)
        {
            using (var sr = new StreamReader(path))
            {
                using (var input = new StringReader(sr.ReadToEnd()))
                {
                    var deserializer = new DeserializerBuilder().IgnoreUnmatchedProperties().Build();
                    return deserializer.Deserialize<ApplicationSetting>(input);
                }
            }
        }
    }
}
