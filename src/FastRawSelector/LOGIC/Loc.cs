using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;

namespace FastRawSelector.LOGIC
{
    /// <summary>
    /// 간단한 다국어 (ko / ja / en).
    /// ApplyLanguage 로 Application.Resources 의 Loc.* 키를 갱신하고,
    /// 코드에서는 Loc.Get / GetFormat 사용.
    /// </summary>
    public static class Loc
    {
        public const string Ko = "ko";
        public const string Ja = "ja";
        public const string En = "en";

        public static string Current { get; private set; } = Ko;

        private static readonly Dictionary<string, Dictionary<string, string>> Tables =
            new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
            {
                { Ko, BuildKo() },
                { Ja, BuildJa() },
                { En, BuildEn() },
            };

        public static string Normalize(string lang)
        {
            if (string.IsNullOrWhiteSpace(lang))
            {
                return Ko;
            }
            lang = lang.Trim().ToLowerInvariant();
            if (lang.StartsWith("ja"))
            {
                return Ja;
            }
            if (lang.StartsWith("en"))
            {
                return En;
            }
            return Ko;
        }

        public static string Get(string key)
        {
            Dictionary<string, string> table;
            if (!Tables.TryGetValue(Current, out table))
            {
                table = Tables[Ko];
            }
            string s;
            if (table != null && table.TryGetValue(key, out s) && s != null)
            {
                return s;
            }
            if (Tables[Ko].TryGetValue(key, out s) && s != null)
            {
                return s;
            }
            return key;
        }

        public static string GetFormat(string key, params object[] args)
        {
            try
            {
                return string.Format(Get(key), args);
            }
            catch
            {
                return Get(key);
            }
        }

        /// <summary>언어 적용 + CultureInfo + Resource 키 갱신.</summary>
        public static void ApplyLanguage(string lang)
        {
            Current = Normalize(lang);
            try
            {
                string culture = Current == Ja ? "ja-JP" : (Current == En ? "en-US" : "ko-KR");
                var ci = CultureInfo.GetCultureInfo(culture);
                CultureInfo.CurrentCulture = ci;
                CultureInfo.CurrentUICulture = ci;
            }
            catch
            {
                // ignore
            }

            Dictionary<string, string> table;
            if (!Tables.TryGetValue(Current, out table))
            {
                table = Tables[Ko];
            }

            var app = Application.Current;
            if (app == null)
            {
                return;
            }
            foreach (var kv in table)
            {
                app.Resources["Loc." + kv.Key] = kv.Value;
            }
            Log.Info("언어 적용: " + Current);
        }

        #region KO
        private static Dictionary<string, string> BuildKo()
        {
            return new Dictionary<string, string>
            {
                // Main menu
                { "MenuFile", "파일" },
                { "MenuOpen", "열기" },
                { "MenuRefresh", "새로고침" },
                { "MenuSettings", "설정" },
                { "MenuExit", "종료" },
                { "MenuEdit", "편집" },
                { "MenuDeselectAll", "전체선택해제" },
                { "MenuOnlySelected", "선택한 사진만 보기" },
                { "MenuExportThumb", "섬네일추출(JPG)" },
                { "MenuRawCopy", "RAW파일복사" },
                { "MenuFolderDivision", "폴더 분류" },
                { "MenuFileTime", "파일시간 변경" },
                { "MenuHelp", "도움말" },
                { "MenuShortcuts", "단축키" },
                { "DropHint", "클릭 또는 파일을 끌어놓으세요" },
                { "TipOpen", "열기(Ctrl+O)" },
                { "TipGrid", "그리드 보기(G)" },
                { "TipSingle", "일반화면모드" },
                { "TipFull", "전체화면모드({0})" },
                { "TipPrev", "이전(왼쪽방향키)" },
                { "TipNext", "다음(오른쪽방향키)" },
                { "TipSelect", "선택({0})" },
                { "TipExif", "EXIF({0})" },
                { "SelectedCount", " ({0}장 선택) " },
                { "NoImageInFolder", "폴더에 표시할 이미지가 없습니다." },
                { "NoImageAllowRawOnly", "RAW 파일이 없습니다. 설정에서 JPEG 등 허용을 켜세요." },

                // Grid
                { "GridFolder", "폴더" },
                { "GridOpenFolder", "이 폴더 열기" },
                { "GridSize", "크기" },
                { "GridRefresh", "새로고침" },

                // Settings
                { "SettingsTitle", "설정" },
                { "TabGeneral", "일반" },
                { "TabDisplay", "화면" },
                { "TabHotkeys", "단축키" },
                { "TabExplorer", "탐색기" },
                { "TabLog", "로그" },
                { "TabLanguage", "언어" },
                { "BasicBehavior", "기본 동작" },
                { "AlwaysOnTop", "항상 위에 고정" },
                { "ShowExif", "EXIF 정보 표시" },
                { "AllowNonRaw", "RAW 외 이미지(JPEG/PNG 등)도 불러오기" },
                { "OpenLastFolder", "시작할 때 마지막 폴더 자동으로 열기" },
                { "UpdateSection", "업데이트" },
                { "AutoCheckUpdate", "시작할 때 새 버전 확인 (GitHub)" },
                { "CheckUpdateNow", "지금 업데이트 확인" },
                { "UpdateHint", "GitHub Releases 의 최신 태그와 비교합니다.\n새 버전이 있으면 다운로드 페이지를 엽니다. (자동 설치 아님)" },
                { "UpdateTitle", "업데이트" },
                { "UpdateAvailable", "새 버전이 있습니다.\n\n현재: {0}\n최신: {1}\n\nGitHub 다운로드 페이지를 열까요?" },
                { "UpdateLatest", "이미 최신 버전입니다.\n({0})" },
                { "UpdateCheckFail", "업데이트 확인에 실패했습니다.\n{0}" },
                { "LastFolder", "최근 폴더: {0}" },
                { "LastFolderNone", "최근 폴더: (없음)" },
                { "Theme", "테마" },
                { "ThemeDark", "다크 (기본)" },
                { "ThemeLight", "라이트" },
                { "ThemeApplyHint", "확인을 누르면 바로 적용됩니다." },
                { "HotkeysEditable", "변경 가능한 키 (단일 키)" },
                { "KeySelect", "선택 토글" },
                { "KeyExif", "EXIF 표시" },
                { "KeyFullScreen", "전체화면" },
                { "HotkeysHint", "← → · Home/End · PageUp/Down · Ctrl 조합 · G(그리드) · Esc 는 고정입니다.\n세 키는 서로 달라야 하며, 확인 후 ToolTip에도 반영됩니다." },
                { "ExplorerTitle", "Windows 탐색기 연동 (현재 사용자)" },
                { "SendTo", "보내기(Send to) 메뉴에 FastRawSelector 등록" },
                { "OpenWith", "연결 프로그램 목록에 등록 (RAW 확장자)" },
                { "ExplorerHint", "관리자 권한 없이 사용자 계정에만 적용됩니다.\n기본 앱을 강제로 바꾸지 않으며, 체크 해제 시 등록이 제거됩니다." },
                { "LogLevel", "로그 레벨" },
                { "LogInfo", "INFO (기본)" },
                { "LogDebug", "DEBUG (상세)" },
                { "LogHint", "DEBUG는 선택 토글·프리패치 등 상세 로그를 기록합니다.\n로그 위치: %AppData%\\Roaming\\FastRawSelector\\logs\\" },
                { "Language", "표시 언어" },
                { "LangKo", "한국어" },
                { "LangJa", "日本語" },
                { "LangEn", "English" },
                { "LangHint", "확인 후 메뉴·버튼 등 UI 언어가 바뀝니다." },
                { "Cancel", "취소" },
                { "OK", "확인" },
                { "Close", "닫기" },
                { "Open", "열기" },
                { "HotkeyConflict", "선택 / EXIF / 전체화면 단축키는 서로 달라야 합니다." },
                { "ShellError", "탐색기 연동 설정 중 오류가 발생했습니다.\n{0}" },

                // Help
                { "HelpTitle", "단축키 · 도움말" },
                { "HelpFile", "파일" },
                { "HelpNav", "탐색" },
                { "HelpSelectView", "선택 · 보기 (설정에서 변경 가능)" },
                { "HelpData", "데이터 위치" },
                { "HelpDataBody", "%AppData%\\Roaming\\FastRawSelector\n설정 · 로그 · 네이티브 DLL\n\n사진 폴더의 FastRawSelector.yaml 은 선택 상태 저장용입니다." },
                { "HelpNavBody", "← / →     이전 / 다음 사진\nPgUp / PgDn  10장 이동\nHome / End  처음 / 마지막" },
                { "HelpFileBody", "Ctrl + O   폴더 열기\nF5          새로고침\nCtrl + Q   종료\nCtrl + ,   설정\nF1          이 도움말\nCtrl + E   섬네일 추출\nCtrl + R   RAW 복사\nCtrl + D   폴더 분류\nCtrl + T   파일시간 변경\nCtrl + S   선택한 사진만 보기" },
                { "HelpCustomBody", "{0}           선택 토글\n{1}           EXIF 표시 전환\n{2}           전체화면\nG           그리드 보기\nEsc         전체화면 해제" },

                // Export
                { "ExportTitle", "섬네일추출" },
                { "ExportTitleFull", "섬네일추출 (JPG)" },
                { "ExportTarget", "출력대상" },
                { "ExportAll", "전부" },
                { "ExportSelected", "선택한 사진만" },
                { "ExportSpecified", "지정한 폴더" },
                { "ExportRawFolder", "(RAW폴더)" },
                { "ExportPath", "출력위치" },
                { "ExportRun", "출력" },

                // Raw copy
                { "RawCopyTitle", "RAW파일복사" },
                { "CopyTarget", "복사대상" },
                { "CopySelected", "선택한 사진" },
                { "CopySpecified", "지정한 폴더" },
                { "CopyRawFolder", "(RAW폴더)" },
                { "CopyJpegFolder", "(JPEG폴더)" },
                { "CopyDest", "복사위치" },
                { "CopyRun", "복사" },

                // Folder division
                { "DivisionTitle", "폴더분류" },
                { "DivisionTitleFull", "폴더 분류" },
                { "DivisionSource", "분류 대상" },
                { "DivisionCurrentAll", "현재 폴더 전체" },
                { "DivisionSelected", "선택한 사진만" },
                { "DivisionSpecified", "지정한 폴더" },
                { "DivisionSourceFolder", "(소스 폴더)" },
                { "DivisionCriteria", "분류 기준" },
                { "DivisionExt", "확장자" },
                { "DivisionDate", "날짜 (파일 수정일)" },
                { "DivisionCamera", "카메라 기종" },
                { "DivisionLens", "렌즈" },
                { "DivisionOutput", "출력 위치" },
                { "DivisionMode", "방식" },
                { "DivisionCopy", "복사 (원본 유지)" },
                { "DivisionMove", "이동" },
                { "DivisionRun", "분류 실행" },

                // File time
                { "FileTimeTitle", "파일시간 변경" },
                { "FileTimeTitleFull", "파일 시간 일괄 변경" },
                { "FileTimeTarget", "대상" },
                { "FileTimeSource", "소스 폴더" },
                { "FileTimeWhich", "변경할 시간" },
                { "FileTimeCreated", "만든 날짜" },
                { "FileTimeModified", "수정한 날짜" },
                { "FileTimeMode", "방식" },
                { "FileTimeAbsolute", "지정 시각으로 통일" },
                { "FileTimeOffset", "상대 이동 (일/시간)" },
                { "FileTimeExif", "EXIF 촬영 시각으로 (가능 시)" },
                { "FileTimeHour", "시" },
                { "FileTimeMinute", "분" },
                { "FileTimeDay", "일" },
                { "FileTimeHours", "시간" },
                { "FileTimeNeg", "(음수 가능)" },
                { "FileTimePreviewHint", "미리보기 (변경 전 → 후). 적용 전 반드시 확인하세요. 원본 파일 시각이 바뀝니다." },
                { "ColFile", "파일" },
                { "ColBefore", "현재 (수정)" },
                { "ColAfter", "변경 후" },
                { "ColNote", "비고" },
                { "Preview", "미리보기" },
                { "Apply", "적용" },

                // Shared messages
                { "AlertTitle", "알림" },
                { "SelectedWithCount", "{0} ({1})" },
                { "SelectedSelecting", "{0} ({1})" },
                { "FolderPickerTarget", "대상 폴더" },
                { "FolderPickerOutput", "출력위치" },
                { "FolderPickerRaw", "RAW폴더" },
                { "FolderPickerJpeg", "JPEG폴더" },
                { "FolderPickerCopy", "복사위치" },
                { "FolderPickerSource", "분류할 소스 폴더" },
                { "FolderPickerDivisionOut", "분류 결과 출력 위치" },

                { "ExportNeedPath", "추출할 위치를 입력해주세요." },
                { "ExportDone", "추출완료했습니다." },
                { "ExportNoSelected", "선택한 사진이 없습니다." },
                { "ExportNeedRaw", "RAW폴더를 입력해주세요." },

                { "CopyNeedPath", "복사할 위치를 입력해주세요." },
                { "CopyDone", "복사완료했습니다." },
                { "CopyFail", "복사실패했습니다." },
                { "CopyNoSelected", "선택한 사진이 없습니다." },
                { "CopyNeedRaw", "RAW폴더를 입력해주세요." },
                { "CopyNeedJpeg", "JPEG폴더를 입력해주세요." },

                { "DivisionNeedOutput", "출력 위치를 입력해주세요." },
                { "DivisionCollectFail", "소스 파일을 모을 수 없습니다.\n{0}" },
                { "DivisionNoFiles", "분류할 파일이 없습니다." },
                { "DivisionMoveConfirm", "원본 파일을 이동합니다 ({0}개).\n계속하시겠습니까?" },
                { "DivisionDone", "분류 완료\n성공 {0} / 실패 {1} / 스킵 {2}\n{3}" },
                { "DivisionError", "분류 중 오류가 발생했습니다.\n{0}" },
                { "DivisionSourceMissing", "소스 폴더가 없습니다." },
                { "UnknownCamera", "UnknownCamera" },
                { "UnknownLens", "UnknownLens" },

                { "FileTimeNoFiles", "대상 파일이 없습니다." },
                { "FileTimeNeedTimestamp", "만든 날짜 또는 수정한 날짜 중 하나 이상 선택하세요." },
                { "FileTimePreviewFail", "미리보기 실패: {0}" },
                { "FileTimeNeedPreview", "먼저 미리보기를 실행하세요." },
                { "FileTimeNothing", "적용할 항목이 없습니다." },
                { "FileTimeConfirm", "{0}개 파일의 시간을 변경합니다.\n이 작업은 되돌리기 어렵습니다. 계속하시겠습니까?" },
                { "FileTimeDone", "적용 완료\n성공 {0} / 실패 {1}" },
                { "FileTimeNeedDate", "날짜를 선택하세요." },
                { "FileTimeHourRange", "시는 0~23 숫자여야 합니다." },
                { "FileTimeMinRange", "분은 0~59 숫자여야 합니다." },
                { "FileTimeDayNum", "일 오프셋이 숫자가 아닙니다." },
                { "FileTimeHourNum", "시간 오프셋이 숫자가 아닙니다." },
                { "FileTimeNeedMode", "방식을 선택하세요." },
                { "FileTimeNoteAbsolute", "지정 시각" },
                { "FileTimeNoteOffset", "오프셋 {0}" },
                { "FileTimeNoExif", "EXIF 시각 없음" },
                { "FileTimeCreatedOnly", " (생성만)" },
                { "FileTimeModifiedOnly", " (수정만)" },

                { "GridNoOpenImage", "열린 이미지가 없습니다. 좌측에서 폴더를 선택하거나 파일을 여세요." },
                // {0}=총 개수 {1}=화면에 표시 중 {2}=디코드 상한px {3}=경로
                { "GridStatus", "{0}장 · 표시 {1} · 품질≤{2}px · {3}" },
            };
        }
        #endregion

        #region JA
        private static Dictionary<string, string> BuildJa()
        {
            return new Dictionary<string, string>
            {
                { "MenuFile", "ファイル" },
                { "MenuOpen", "開く" },
                { "MenuRefresh", "更新" },
                { "MenuSettings", "設定" },
                { "MenuExit", "終了" },
                { "MenuEdit", "編集" },
                { "MenuDeselectAll", "すべて選択解除" },
                { "MenuOnlySelected", "選択した写真のみ表示" },
                { "MenuExportThumb", "サムネイル書き出し(JPG)" },
                { "MenuRawCopy", "RAWファイルコピー" },
                { "MenuFolderDivision", "フォルダ分類" },
                { "MenuFileTime", "ファイル日時の変更" },
                { "MenuHelp", "ヘルプ" },
                { "MenuShortcuts", "ショートカット" },
                { "DropHint", "クリックまたはファイルをドロップ" },
                { "TipOpen", "開く(Ctrl+O)" },
                { "TipGrid", "グリッド表示(G)" },
                { "TipSingle", "通常表示" },
                { "TipFull", "全画面({0})" },
                { "TipPrev", "前へ(←)" },
                { "TipNext", "次へ(→)" },
                { "TipSelect", "選択({0})" },
                { "TipExif", "EXIF({0})" },
                { "SelectedCount", " ({0}枚選択) " },
                { "NoImageInFolder", "表示できる画像がありません。" },
                { "NoImageAllowRawOnly", "RAWがありません。設定でJPEG等を許可してください。" },

                { "GridFolder", "フォルダ" },
                { "GridOpenFolder", "このフォルダを開く" },
                { "GridSize", "サイズ" },
                { "GridRefresh", "更新" },

                { "SettingsTitle", "設定" },
                { "TabGeneral", "一般" },
                { "TabDisplay", "表示" },
                { "TabHotkeys", "ショートカット" },
                { "TabExplorer", "エクスプローラー" },
                { "TabLog", "ログ" },
                { "TabLanguage", "言語" },
                { "BasicBehavior", "基本動作" },
                { "AlwaysOnTop", "常に手前に表示" },
                { "ShowExif", "EXIF情報を表示" },
                { "AllowNonRaw", "RAW以外(JPEG/PNG等)も読み込む" },
                { "OpenLastFolder", "起動時に最後のフォルダを開く" },
                { "UpdateSection", "アップデート" },
                { "AutoCheckUpdate", "起動時に新バージョンを確認 (GitHub)" },
                { "CheckUpdateNow", "今すぐアップデート確認" },
                { "UpdateHint", "GitHub Releases の最新タグと比較します。\n新バージョンがあればダウンロードページを開きます。(自動インストールではありません)" },
                { "UpdateTitle", "アップデート" },
                { "UpdateAvailable", "新しいバージョンがあります。\n\n現在: {0}\n最新: {1}\n\nGitHub のダウンロードページを開きますか？" },
                { "UpdateLatest", "すでに最新バージョンです。\n({0})" },
                { "UpdateCheckFail", "アップデート確認に失敗しました。\n{0}" },
                { "LastFolder", "最近のフォルダ: {0}" },
                { "LastFolderNone", "最近のフォルダ: (なし)" },
                { "Theme", "テーマ" },
                { "ThemeDark", "ダーク (既定)" },
                { "ThemeLight", "ライト" },
                { "ThemeApplyHint", "OKで直ちに適用されます。" },
                { "HotkeysEditable", "変更可能なキー (単一キー)" },
                { "KeySelect", "選択トグル" },
                { "KeyExif", "EXIF表示" },
                { "KeyFullScreen", "全画面" },
                { "HotkeysHint", "← → · Home/End · PageUp/Down · Ctrl 組み合わせ · G(グリッド) · Esc は固定です。\n3つのキーは重複不可。OK後にツールチップへ反映されます。" },
                { "ExplorerTitle", "エクスプローラー連携 (現在のユーザー)" },
                { "SendTo", "送るメニューに FastRawSelector を登録" },
                { "OpenWith", "プログラムから開くに登録 (RAW拡張子)" },
                { "ExplorerHint", "管理者権限不要でユーザー単位に適用されます。\n既定アプリの強制変更はしません。オフで登録解除。" },
                { "LogLevel", "ログレベル" },
                { "LogInfo", "INFO (既定)" },
                { "LogDebug", "DEBUG (詳細)" },
                { "LogHint", "DEBUGは選択トグルやプリフェッチ等を記録します。\n場所: %AppData%\\Roaming\\FastRawSelector\\logs\\" },
                { "Language", "表示言語" },
                { "LangKo", "한국어" },
                { "LangJa", "日本語" },
                { "LangEn", "English" },
                { "LangHint", "OK後にメニューやボタンの言語が変わります。" },
                { "Cancel", "キャンセル" },
                { "OK", "OK" },
                { "Close", "閉じる" },
                { "Open", "開く" },
                { "HotkeyConflict", "選択 / EXIF / 全画面のショートカットはそれぞれ異なるキーにしてください。" },
                { "ShellError", "エクスプローラー連携の設定中にエラーが発生しました。\n{0}" },

                { "HelpTitle", "ショートカット · ヘルプ" },
                { "HelpFile", "ファイル" },
                { "HelpNav", "移動" },
                { "HelpSelectView", "選択 · 表示 (設定で変更可)" },
                { "HelpData", "データ場所" },
                { "HelpDataBody", "%AppData%\\Roaming\\FastRawSelector\n設定 · ログ · ネイティブ DLL\n\n写真フォルダの FastRawSelector.yaml は選択状態の保存用です。" },
                { "HelpNavBody", "← / →     前 / 次の写真\nPgUp / PgDn  10枚移動\nHome / End  最初 / 最後" },
                { "HelpFileBody", "Ctrl + O   フォルダを開く\nF5          更新\nCtrl + Q   終了\nCtrl + ,   設定\nF1          このヘルプ\nCtrl + E   サムネイル書き出し\nCtrl + R   RAWコピー\nCtrl + D   フォルダ分類\nCtrl + T   ファイル日時変更\nCtrl + S   選択した写真のみ" },
                { "HelpCustomBody", "{0}           選択トグル\n{1}           EXIF表示切替\n{2}           全画面\nG           グリッド表示\nEsc         全画面解除" },

                { "ExportTitle", "サムネイル書き出し" },
                { "ExportTitleFull", "サムネイル書き出し (JPG)" },
                { "ExportTarget", "出力対象" },
                { "ExportAll", "すべて" },
                { "ExportSelected", "選択した写真のみ" },
                { "ExportSpecified", "指定フォルダ" },
                { "ExportRawFolder", "(RAWフォルダ)" },
                { "ExportPath", "出力先" },
                { "ExportRun", "出力" },

                { "RawCopyTitle", "RAWファイルコピー" },
                { "CopyTarget", "コピー対象" },
                { "CopySelected", "選択した写真" },
                { "CopySpecified", "指定フォルダ" },
                { "CopyRawFolder", "(RAWフォルダ)" },
                { "CopyJpegFolder", "(JPEGフォルダ)" },
                { "CopyDest", "コピー先" },
                { "CopyRun", "コピー" },

                { "DivisionTitle", "フォルダ分類" },
                { "DivisionTitleFull", "フォルダ分類" },
                { "DivisionSource", "分類対象" },
                { "DivisionCurrentAll", "現在のフォルダ全体" },
                { "DivisionSelected", "選択した写真のみ" },
                { "DivisionSpecified", "指定フォルダ" },
                { "DivisionSourceFolder", "(ソースフォルダ)" },
                { "DivisionCriteria", "分類基準" },
                { "DivisionExt", "拡張子" },
                { "DivisionDate", "日付 (更新日)" },
                { "DivisionCamera", "カメラ機種" },
                { "DivisionLens", "レンズ" },
                { "DivisionOutput", "出力先" },
                { "DivisionMode", "方式" },
                { "DivisionCopy", "コピー (原本を残す)" },
                { "DivisionMove", "移動" },
                { "DivisionRun", "分類実行" },

                { "FileTimeTitle", "ファイル日時の変更" },
                { "FileTimeTitleFull", "ファイル日時の一括変更" },
                { "FileTimeTarget", "対象" },
                { "FileTimeSource", "ソースフォルダ" },
                { "FileTimeWhich", "変更する日時" },
                { "FileTimeCreated", "作成日時" },
                { "FileTimeModified", "更新日時" },
                { "FileTimeMode", "方式" },
                { "FileTimeAbsolute", "指定時刻に統一" },
                { "FileTimeOffset", "相対移動 (日/時間)" },
                { "FileTimeExif", "EXIF撮影時刻へ (可能な場合)" },
                { "FileTimeHour", "時" },
                { "FileTimeMinute", "分" },
                { "FileTimeDay", "日" },
                { "FileTimeHours", "時間" },
                { "FileTimeNeg", "(負の値可)" },
                { "FileTimePreviewHint", "プレビュー (変更前 → 後)。適用前に必ず確認してください。元ファイルの日時が変わります。" },
                { "ColFile", "ファイル" },
                { "ColBefore", "現在 (更新)" },
                { "ColAfter", "変更後" },
                { "ColNote", "備考" },
                { "Preview", "プレビュー" },
                { "Apply", "適用" },

                { "AlertTitle", "通知" },
                { "SelectedWithCount", "{0} ({1})" },
                { "SelectedSelecting", "{0} ({1})" },
                { "FolderPickerTarget", "対象フォルダ" },
                { "FolderPickerOutput", "出力先" },
                { "FolderPickerRaw", "RAWフォルダ" },
                { "FolderPickerJpeg", "JPEGフォルダ" },
                { "FolderPickerCopy", "コピー先" },
                { "FolderPickerSource", "分類元フォルダ" },
                { "FolderPickerDivisionOut", "分類結果の出力先" },

                { "ExportNeedPath", "出力先を入力してください。" },
                { "ExportDone", "書き出しが完了しました。" },
                { "ExportNoSelected", "選択した写真がありません。" },
                { "ExportNeedRaw", "RAWフォルダを入力してください。" },

                { "CopyNeedPath", "コピー先を入力してください。" },
                { "CopyDone", "コピーが完了しました。" },
                { "CopyFail", "コピーに失敗しました。" },
                { "CopyNoSelected", "選択した写真がありません。" },
                { "CopyNeedRaw", "RAWフォルダを入力してください。" },
                { "CopyNeedJpeg", "JPEGフォルダを入力してください。" },

                { "DivisionNeedOutput", "出力先を入力してください。" },
                { "DivisionCollectFail", "ソースファイルを集められません。\n{0}" },
                { "DivisionNoFiles", "分類するファイルがありません。" },
                { "DivisionMoveConfirm", "元ファイルを移動します ({0}件)。\n続行しますか？" },
                { "DivisionDone", "分類完了\n成功 {0} / 失敗 {1} / スキップ {2}\n{3}" },
                { "DivisionError", "分類中にエラーが発生しました。\n{0}" },
                { "DivisionSourceMissing", "ソースフォルダがありません。" },
                { "UnknownCamera", "UnknownCamera" },
                { "UnknownLens", "UnknownLens" },

                { "FileTimeNoFiles", "対象ファイルがありません。" },
                { "FileTimeNeedTimestamp", "作成日時または更新日時を1つ以上選んでください。" },
                { "FileTimePreviewFail", "プレビュー失敗: {0}" },
                { "FileTimeNeedPreview", "先にプレビューを実行してください。" },
                { "FileTimeNothing", "適用する項目がありません。" },
                { "FileTimeConfirm", "{0}件のファイル日時を変更します。\n元に戻すのは困難です。続行しますか？" },
                { "FileTimeDone", "適用完了\n成功 {0} / 失敗 {1}" },
                { "FileTimeNeedDate", "日付を選んでください。" },
                { "FileTimeHourRange", "時は0〜23の数字です。" },
                { "FileTimeMinRange", "分は0〜59の数字です。" },
                { "FileTimeDayNum", "日オフセットが数字ではありません。" },
                { "FileTimeHourNum", "時間オフセットが数字ではありません。" },
                { "FileTimeNeedMode", "方式を選んでください。" },
                { "FileTimeNoteAbsolute", "指定時刻" },
                { "FileTimeNoteOffset", "オフセット {0}" },
                { "FileTimeNoExif", "EXIF時刻なし" },
                { "FileTimeCreatedOnly", " (作成のみ)" },
                { "FileTimeModifiedOnly", " (更新のみ)" },

                { "GridNoOpenImage", "開いている画像がありません。左のフォルダを選ぶかファイルを開いてください。" },
                { "GridStatus", "{0}枚 · 表示 {1} · 品質≤{2}px · {3}" },
            };
        }
        #endregion

        #region EN
        private static Dictionary<string, string> BuildEn()
        {
            return new Dictionary<string, string>
            {
                { "MenuFile", "File" },
                { "MenuOpen", "Open" },
                { "MenuRefresh", "Refresh" },
                { "MenuSettings", "Settings" },
                { "MenuExit", "Exit" },
                { "MenuEdit", "Edit" },
                { "MenuDeselectAll", "Deselect all" },
                { "MenuOnlySelected", "Show selected only" },
                { "MenuExportThumb", "Export thumbnails (JPG)" },
                { "MenuRawCopy", "Copy RAW files" },
                { "MenuFolderDivision", "Sort into folders" },
                { "MenuFileTime", "Change file times" },
                { "MenuHelp", "Help" },
                { "MenuShortcuts", "Shortcuts" },
                { "DropHint", "Click or drop files here" },
                { "TipOpen", "Open (Ctrl+O)" },
                { "TipGrid", "Grid view (G)" },
                { "TipSingle", "Single view" },
                { "TipFull", "Fullscreen ({0})" },
                { "TipPrev", "Previous (Left)" },
                { "TipNext", "Next (Right)" },
                { "TipSelect", "Select ({0})" },
                { "TipExif", "EXIF ({0})" },
                { "SelectedCount", " ({0} selected) " },
                { "NoImageInFolder", "No images to display in this folder." },
                { "NoImageAllowRawOnly", "No RAW files. Enable non-RAW images in Settings." },

                { "GridFolder", "Folder" },
                { "GridOpenFolder", "Open this folder" },
                { "GridSize", "Size" },
                { "GridRefresh", "Refresh" },

                { "SettingsTitle", "Settings" },
                { "TabGeneral", "General" },
                { "TabDisplay", "Display" },
                { "TabHotkeys", "Hotkeys" },
                { "TabExplorer", "Explorer" },
                { "TabLog", "Log" },
                { "TabLanguage", "Language" },
                { "BasicBehavior", "Basics" },
                { "AlwaysOnTop", "Always on top" },
                { "ShowExif", "Show EXIF info" },
                { "AllowNonRaw", "Also load non-RAW images (JPEG/PNG, etc.)" },
                { "OpenLastFolder", "Open last folder on startup" },
                { "UpdateSection", "Updates" },
                { "AutoCheckUpdate", "Check for new version on startup (GitHub)" },
                { "CheckUpdateNow", "Check for updates now" },
                { "UpdateHint", "Compares with the latest GitHub Releases tag.\nIf a newer version exists, opens the download page. (No auto-install)" },
                { "UpdateTitle", "Update" },
                { "UpdateAvailable", "A new version is available.\n\nCurrent: {0}\nLatest: {1}\n\nOpen the GitHub download page?" },
                { "UpdateLatest", "You are on the latest version.\n({0})" },
                { "UpdateCheckFail", "Update check failed.\n{0}" },
                { "LastFolder", "Last folder: {0}" },
                { "LastFolderNone", "Last folder: (none)" },
                { "Theme", "Theme" },
                { "ThemeDark", "Dark (default)" },
                { "ThemeLight", "Light" },
                { "ThemeApplyHint", "Applied immediately when you click OK." },
                { "HotkeysEditable", "Customizable keys (single key)" },
                { "KeySelect", "Toggle select" },
                { "KeyExif", "Toggle EXIF" },
                { "KeyFullScreen", "Fullscreen" },
                { "HotkeysHint", "← → · Home/End · PageUp/Down · Ctrl combos · G (grid) · Esc are fixed.\nThe three keys must be unique. Tooltips update after OK." },
                { "ExplorerTitle", "Windows Explorer integration (current user)" },
                { "SendTo", "Add FastRawSelector to Send to menu" },
                { "OpenWith", "Register in Open with list (RAW extensions)" },
                { "ExplorerHint", "Applies to this user only (no admin).\nDoes not force the default app. Uncheck to remove." },
                { "LogLevel", "Log level" },
                { "LogInfo", "INFO (default)" },
                { "LogDebug", "DEBUG (verbose)" },
                { "LogHint", "DEBUG logs select toggles, prefetch, etc.\nPath: %AppData%\\Roaming\\FastRawSelector\\logs\\" },
                { "Language", "UI language" },
                { "LangKo", "한국어" },
                { "LangJa", "日本語" },
                { "LangEn", "English" },
                { "LangHint", "Menus and buttons update after OK." },
                { "Cancel", "Cancel" },
                { "OK", "OK" },
                { "Close", "Close" },
                { "Open", "Open" },
                { "HotkeyConflict", "Select / EXIF / Fullscreen hotkeys must be different." },
                { "ShellError", "Failed to update Explorer integration.\n{0}" },

                { "HelpTitle", "Shortcuts · Help" },
                { "HelpFile", "File" },
                { "HelpNav", "Navigate" },
                { "HelpSelectView", "Select · View (changeable in Settings)" },
                { "HelpData", "Data location" },
                { "HelpDataBody", "%AppData%\\Roaming\\FastRawSelector\nSettings · logs · native DLLs\n\nFastRawSelector.yaml in a photo folder stores selection state." },
                { "HelpNavBody", "← / →     Previous / next\nPgUp / PgDn  Jump 10\nHome / End  First / last" },
                { "HelpFileBody", "Ctrl + O   Open folder\nF5          Refresh\nCtrl + Q   Exit\nCtrl + ,   Settings\nF1          This help\nCtrl + E   Export thumbnails\nCtrl + R   Copy RAW\nCtrl + D   Sort folders\nCtrl + T   Change file times\nCtrl + S   Selected only" },
                { "HelpCustomBody", "{0}           Toggle select\n{1}           Toggle EXIF\n{2}           Fullscreen\nG           Grid view\nEsc         Exit fullscreen" },

                { "ExportTitle", "Export thumbnails" },
                { "ExportTitleFull", "Export thumbnails (JPG)" },
                { "ExportTarget", "Source" },
                { "ExportAll", "All" },
                { "ExportSelected", "Selected only" },
                { "ExportSpecified", "Specified folder" },
                { "ExportRawFolder", "(RAW folder)" },
                { "ExportPath", "Output folder" },
                { "ExportRun", "Export" },

                { "RawCopyTitle", "Copy RAW files" },
                { "CopyTarget", "Source" },
                { "CopySelected", "Selected photos" },
                { "CopySpecified", "Specified folder" },
                { "CopyRawFolder", "(RAW folder)" },
                { "CopyJpegFolder", "(JPEG folder)" },
                { "CopyDest", "Destination" },
                { "CopyRun", "Copy" },

                { "DivisionTitle", "Sort into folders" },
                { "DivisionTitleFull", "Sort into folders" },
                { "DivisionSource", "Source" },
                { "DivisionCurrentAll", "Current folder (all)" },
                { "DivisionSelected", "Selected only" },
                { "DivisionSpecified", "Specified folder" },
                { "DivisionSourceFolder", "(Source folder)" },
                { "DivisionCriteria", "Criteria" },
                { "DivisionExt", "Extension" },
                { "DivisionDate", "Date (modified)" },
                { "DivisionCamera", "Camera model" },
                { "DivisionLens", "Lens" },
                { "DivisionOutput", "Output folder" },
                { "DivisionMode", "Mode" },
                { "DivisionCopy", "Copy (keep originals)" },
                { "DivisionMove", "Move" },
                { "DivisionRun", "Run" },

                { "FileTimeTitle", "Change file times" },
                { "FileTimeTitleFull", "Batch change file times" },
                { "FileTimeTarget", "Target" },
                { "FileTimeSource", "Source folder" },
                { "FileTimeWhich", "Timestamps to change" },
                { "FileTimeCreated", "Created" },
                { "FileTimeModified", "Modified" },
                { "FileTimeMode", "Mode" },
                { "FileTimeAbsolute", "Set all to a fixed time" },
                { "FileTimeOffset", "Relative shift (days/hours)" },
                { "FileTimeExif", "From EXIF capture time (if available)" },
                { "FileTimeHour", "Hour" },
                { "FileTimeMinute", "Min" },
                { "FileTimeDay", "Days" },
                { "FileTimeHours", "Hours" },
                { "FileTimeNeg", "(negative OK)" },
                { "FileTimePreviewHint", "Preview (before → after). Confirm before apply. Original file times will change." },
                { "ColFile", "File" },
                { "ColBefore", "Current (modified)" },
                { "ColAfter", "After" },
                { "ColNote", "Note" },
                { "Preview", "Preview" },
                { "Apply", "Apply" },

                { "AlertTitle", "Notice" },
                { "SelectedWithCount", "{0} ({1})" },
                { "SelectedSelecting", "{0} ({1})" },
                { "FolderPickerTarget", "Target folder" },
                { "FolderPickerOutput", "Output folder" },
                { "FolderPickerRaw", "RAW folder" },
                { "FolderPickerJpeg", "JPEG folder" },
                { "FolderPickerCopy", "Copy destination" },
                { "FolderPickerSource", "Source folder to sort" },
                { "FolderPickerDivisionOut", "Sort output folder" },

                { "ExportNeedPath", "Please enter an export location." },
                { "ExportDone", "Export finished." },
                { "ExportNoSelected", "No photos selected." },
                { "ExportNeedRaw", "Please enter a RAW folder." },

                { "CopyNeedPath", "Please enter a copy destination." },
                { "CopyDone", "Copy finished." },
                { "CopyFail", "Copy failed." },
                { "CopyNoSelected", "No photos selected." },
                { "CopyNeedRaw", "Please enter a RAW folder." },
                { "CopyNeedJpeg", "Please enter a JPEG folder." },

                { "DivisionNeedOutput", "Please enter an output location." },
                { "DivisionCollectFail", "Could not collect source files.\n{0}" },
                { "DivisionNoFiles", "No files to sort." },
                { "DivisionMoveConfirm", "Move {0} original file(s)?\nContinue?" },
                { "DivisionDone", "Sort finished\nOK {0} / Fail {1} / Skip {2}\n{3}" },
                { "DivisionError", "Error while sorting.\n{0}" },
                { "DivisionSourceMissing", "Source folder not found." },
                { "UnknownCamera", "UnknownCamera" },
                { "UnknownLens", "UnknownLens" },

                { "FileTimeNoFiles", "No target files." },
                { "FileTimeNeedTimestamp", "Select created and/or modified time." },
                { "FileTimePreviewFail", "Preview failed: {0}" },
                { "FileTimeNeedPreview", "Run preview first." },
                { "FileTimeNothing", "Nothing to apply." },
                { "FileTimeConfirm", "Change timestamps on {0} file(s)?\nThis is hard to undo. Continue?" },
                { "FileTimeDone", "Applied\nOK {0} / Fail {1}" },
                { "FileTimeNeedDate", "Please pick a date." },
                { "FileTimeHourRange", "Hour must be 0–23." },
                { "FileTimeMinRange", "Minute must be 0–59." },
                { "FileTimeDayNum", "Day offset is not a number." },
                { "FileTimeHourNum", "Hour offset is not a number." },
                { "FileTimeNeedMode", "Please select a mode." },
                { "FileTimeNoteAbsolute", "Fixed time" },
                { "FileTimeNoteOffset", "Offset {0}" },
                { "FileTimeNoExif", "No EXIF time" },
                { "FileTimeCreatedOnly", " (created only)" },
                { "FileTimeModifiedOnly", " (modified only)" },

                { "GridNoOpenImage", "No open image. Pick a folder on the left or open a file." },
                { "GridStatus", "{0} · shown {1} · quality≤{2}px · {3}" },
            };
        }
        #endregion
    }
}
