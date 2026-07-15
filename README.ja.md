<p align="center">
  <img src="src/FastRawSelector/FastRawSelector.ico" width="96" height="96" alt="FastRawSelector アイコン"/>
</p>

<h1 align="center">FastRawSelector</h1>

<p align="center">
  <strong>Windows 向け 高速 RAW セレクト（culling）ツール</strong><br/>
  重い現像ソフトを開かずに、見て・選んで・書き出す。
</p>

<p align="center">
  <a href="README.md">English</a> ·
  <a href="README.ko.md">한국어</a> ·
  <a href="README.ja.md"><b>日本語</b></a>
</p>

<p align="center">
  <a href="https://github.com/HandaJun/FastRawSelector/releases/latest"><img alt="Release" src="https://img.shields.io/github/v/release/HandaJun/FastRawSelector?style=for-the-badge&color=00BCD4"/></a>
  <a href="https://github.com/HandaJun/FastRawSelector/releases"><img alt="Downloads" src="https://img.shields.io/github/downloads/HandaJun/FastRawSelector/total?style=for-the-badge&color=26A69A"/></a>
  <img alt="Platform" src="https://img.shields.io/badge/Platform-Windows-0078D6?style=for-the-badge&logo=windows&logoColor=white"/>
  <img alt=".NET" src="https://img.shields.io/badge/.NET%20Framework-4.6.2+-512BD4?style=for-the-badge&logo=dotnet&logoColor=white"/>
  <img alt="UI" src="https://img.shields.io/badge/UI-EN%20%7C%20KO%20%7C%20JA-FF7043?style=for-the-badge"/>
</p>

<p align="center">
  <a href="https://github.com/HandaJun/FastRawSelector/releases/latest"><img src="https://img.shields.io/badge/⬇%20最新版をダウンロード-1.0.0-00BCD4?style=for-the-badge"/></a>
</p>

---

## なぜ FastRawSelector？

何百枚もの RAW を現像ソフトだけでめくると、**選ぶだけ**でも時間がかかります。  
FastRawSelector は、そのループ向けの **軽量 WPF ビューア**です。

| | |
|:--|:--|
| **速さ** | libraw / exiv2 の埋め込みプレビュー — 毎回 full demosaic しない |
| **集中** | キーボード中心の移動・選択・全画面 |
| **軽さ** | ポータブル単一 `exe` + 設定は AppData |
| **安全** | フォルダごとの選択状態保存。書き出し／コピーまで原本はそのまま |

---

## 主な機能

### 表示・選択
- **シングル / 全画面 / グリッド**  
- **← →** 1枚、**PgUp / PgDn** 10枚、**Home / End**  
- **選択トグル**（既定 `B`）、選択数、「選択のみ」  
- **EXIF パネル**（ショートカット変更可）  
- **ダーク / ライト** テーマ  

### 作業
- **サムネイル JPG 書き出し** — 全部 / 選択 / フォルダ  
- **RAW コピー** — 選択、または JPEG 名マッチ  
- **フォルダ振り分け** — 拡張子・日付・カメラ・レンズ  
- **ファイル時刻の一括変更**  

### アプリ
- UI: **日本語 · 한국어 · English**  
- エクスプローラー **送る / プログラムから開く**（任意）  
- **GitHub Releases の更新確認**（設定で ON/OFF）  
- ウィンドウ位置・サイズ、最近のフォルダ  

---

## クイックスタート

1. **[Releases → Latest](https://github.com/HandaJun/FastRawSelector/releases/latest)** から  
   **`FastRawSelector-v*.zip`** を入手  
2. 展開して **`FastRawSelector.exe`** を実行  
3. 必要なら  
   **[.NET Framework 4.6.2+](https://dotnet.microsoft.com/download/dotnet-framework/net462)** をインストール  
4. **Ctrl+O**、RAW をドラッグ、または「送る」でフォルダを開く  

> **更新:** 新しい zip で `exe` を差し替え。  
> 設定は `%AppData%\Roaming\FastRawSelector\` に残ります。

---

## 既定ショートカット

| キー | 動作 |
|:--|:--|
| `←` / `→` | 前 / 次 |
| `PgUp` / `PgDn` | ±10 枚 |
| `Home` / `End` | 最初 / 最後 |
| `B` * | 選択トグル |
| `I` * | EXIF |
| `F` * | 全画面 |
| `G` | グリッド |
| `Ctrl+O` | フォルダを開く |
| `Ctrl+,` | 設定 |
| `F1` | ヘルプ |
| `Esc` | 全画面解除 |

\* **設定 → ショートカット** で変更可能

---

## 動作環境

| 項目 | 内容 |
|:--|:--|
| OS | Windows 7 SP1 以降（WPF） |
| ランタイム | .NET Framework **4.6.2** 以上 |
| インストール | ポータブル zip（インストーラ不要） |
| ネットワーク | 任意（更新確認時） |

---

## ソースからビルド

```bat
msbuild src\FastRawSelector.sln /t:Restore /p:Configuration=Release
msbuild src\FastRawSelector.sln /p:Configuration=Release /p:Platform="Any CPU"
```

出力:

```text
src\FastRawSelector\bin\Release\FastRawSelector.exe
```

---

## 構成

```text
src/
  FastRawSelector.sln
  FastRawSelector/          # WPF アプリ
```

公開リポジトリの製品ソースは **`src`** です。

---

## ライセンス

- アプリ: [LICENSE](LICENSE)  
- ネイティブ: **libraw**, **exiv2** など — 各ライセンスに従ってください  
- UI: MaterialDesignThemes、MahApps アイコンなど（NuGet）

---

## サポート

- Issues: [GitHub Issues](https://github.com/HandaJun/FastRawSelector/issues)  
- 配布: [Releases](https://github.com/HandaJun/FastRawSelector/releases)

<p align="center">
  <sub>まず選んで、あとから現像。</sub>
</p>
