# Background Service Sample

.NET Generic Host の `BackgroundService` を GUI アプリに統合するサンプルです。
`PeriodicTimer` で定期実行されるバックグラウンドタスクの進捗とログを、
UI（進捗バー・ログ一覧）にリアルタイム反映します。

GUI には WPF ではなく [Avalonia UI](https://avaloniaui.net/) を採用しているため、
**Windows / macOS / Linux** のいずれでも動作します。

## 必要環境

- .NET SDK 10.0.300（`mise.toml` と `global.json` で固定）

[mise](https://mise.jdx.dev/) を使う場合、リポジトリ直下で SDK を自動取得できます。

```bash
mise install
```

## 実行

```bash
mise exec -- dotnet run --project src/BackgroundServiceSample
```

ウィンドウの **ステータス** タブで **開始** を押すと、既定では 3 秒間隔でタスクが実行され、
進捗バーが 0 → 100% に進みます。**停止** で待機状態に戻ります。

**設定** タブでは実行間隔を 1〜3600 秒の整数で指定し、**保存** できます。
変更はアプリの再起動後に反映されます。**元に戻す** は未保存の編集を破棄して保存済みの値に戻します。
保存先はユーザーの LocalApplicationData 配下の `BackgroundServiceSample/settings.json`
（Windows では通常 `%LOCALAPPDATA%\BackgroundServiceSample\settings.json`）です。
読み込みに失敗した場合は既定値で起動し、設定画面に警告を表示します。

設定の保存・入力チェックなどのテストは GUI なしで実行できます。

```bash
mise exec -- dotnet test tests/BackgroundServiceSample.Tests
```

## 構成

Avalonia アプリと UI に依存しない Workers ライブラリを別プロジェクトに分けています。
アプリが Workers を参照し、Generic Host の起動・終了と DI 登録はアプリ側で管理します。

ViewModel は `CommunityToolkit.Mvvm` の `ObservableObject` を継承し、
`SetProperty` で変更を通知します。コマンドは `[RelayCommand]` から生成します。
開始・停止ボタンの有効状態は `CanExecute` と `NotifyCanExecuteChanged` で更新します。
状態や進捗などは ViewModel 内からだけ変更できるよう、プロパティの private setter を維持しています。
Workers プロジェクトは MVVM ライブラリに依存しません。

| ファイル | 役割 |
| --- | --- |
| `src/BackgroundServiceSample/Program.cs` | Generic Host を起動し、DI コンテナを Avalonia に橋渡しするエントリポイント |
| `src/BackgroundServiceSample.Workers/PeriodicTaskService.cs` | `PeriodicTimer` で定期起動する `BackgroundService`。アクティブ時のみ擬似タスクを実行 |
| `src/BackgroundServiceSample.Workers/WorkerCoordinator.cs` | UI とサービス間で開始/停止の制御・進捗・ログを仲介する singleton |
| `src/BackgroundServiceSample/Features/Status/StatusViewModel.cs` | サービスのイベントを UI スレッドへマーシャリングし、状態・進捗・ログに反映 |
| `src/BackgroundServiceSample/Features/Status/StatusView.axaml` | 開始/停止ボタン、進捗バー、ログ一覧を持つステータス画面 |
| `src/BackgroundServiceSample/Features/Settings/SettingsViewModel.cs` | 実行間隔の編集・検証・保存 |
| `src/BackgroundServiceSample/Features/Settings/SettingsView.axaml` | 設定画面 |
| `src/BackgroundServiceSample/Shell/MainWindow.axaml` | 各機能の画面をタブに配置するメインウィンドウ |

アプリの UI は機能ごとに View と ViewModel をまとめています。

```text
src/BackgroundServiceSample/
  Features/
    Status/       # StatusView.axaml / StatusView.axaml.cs / StatusViewModel.cs
    Settings/     # SettingsView.axaml / SettingsView.axaml.cs / SettingsViewModel.cs
  Shell/          # MainWindow と、各機能をまとめる MainWindowViewModel
  App.axaml
  Program.cs
```

Status と Settings は互いの ViewModel を参照せず、必要な Workers のサービス・設定を DI で受け取ります。

アプリと Workers をまとめてビルドするには、次を実行します。

```bash
mise exec -- dotnet build src/BackgroundServiceSample
```

## Windows E2E テスト

`tests/BackgroundServiceSample.E2E` は xUnit と FlaUI.UIA3 を使い、公開した実際のアプリを
別プロセスとして起動します。バックグラウンドサービスも実物を動かします。

E2E は画面操作・実行基盤・シナリオをディレクトリで分けています。

```text
tests/BackgroundServiceSample.E2E/
  Screens/          # SettingsScreen / StatusScreen: 要素検索、入力・クリック、表示値の取得
  Infrastructure/   # AppSession: 起動・終了、待機、診断情報の保存
  Tests/            # SettingsTests / WorkerLifecycleTests: 操作手順と期待値の検証
```

テストは `app.OpenSettings()` / `app.OpenStatus()` で Screen Object を取得します。
AutomationId と FlaUI の要素操作は各 Screen に閉じ込め、UI 要素は操作・取得時に検索します。

Windows に .NET SDK 10.0.300 と .NET 10 Desktop Runtime をインストールし、
ログイン済み・ロックされていないデスクトップの PowerShell で実行してください。
Appium サーバーは不要です。テスト中はマウスを操作するため、他の操作を控えてください。

```powershell
./scripts/test-e2e.ps1
# Windows ARM64 の場合
./scripts/test-e2e.ps1 -Runtime win-arm64
```

スクリプトは Release ビルドの発行とテストを行います。mise を利用する場合は
`mise exec -- pwsh -File scripts/test-e2e.ps1` でも実行できます。

検証する内容:

- メインウィンドウが表示され、初期状態と開始・停止ボタンの有効状態が正しい
- 開始ボタンからサービスが動き、状態・進捗・開始ログが UI に反映される
- 停止後、実行中の処理は完了し、完了ログと進捗 100% が表示される
- その後 6 秒間（定期間隔の 2 回分）、新しいタスクのログが増えない
- ウィンドウを閉じると Host を含むアプリのプロセスが正常終了する
- 設定画面の入力チェック・保存・編集の取り消しと、再起動後の設定読み込み

E2E は毎回専用の一時ディレクトリを `BACKGROUND_SERVICE_SAMPLE_SETTINGS_DIR` で指定し、
普段の設定を変更しません。設定保存後と再起動後の画面画像も結果に保存します。

要素は `AutomationProperties.AutomationId` で検索し、状態変化は最大 20 秒待機します。
テストは並列実行しません。結果は `artifacts/e2e/results` に保存します。
失敗時にはプロセスログと例外、取得可能な場合はウィンドウ画像と UI 要素一覧も保存します。

macOS / Linux では FlaUI の E2E は実行できません。
CI に組み込む場合も、GUI 操作できる Windows セッションが必要です。

## GitHub Actions

[EditorConfig](https://github.com/Wataru-Toriumi/Background-Service-Sample/actions/workflows/editorconfig.yml)
は `main` への push・Pull Request・手動実行で、`.editorconfig` に沿った書式を検証します。
E2E とは独立した Ubuntu ジョブで動き、違反がある場合は失敗します。

チェック内容は UTF-8、LF、最終行の改行、末尾空白、スペースによるインデントです。
C#・PowerShell は 4 スペース、XAML・プロジェクトファイル・JSON・YAML・TOML・Markdown は 2 スペースです。
Markdown の意図的な改行用末尾スペースは許容します。生成物はチェック対象外です。
`.gitattributes` で Windows のチェックアウト時も LF に揃えます。
共通の書式チェックに加え、同じワークフローの **C# IDE diagnostics** ジョブで
全プロジェクトの C# コードスタイルも検証します。PowerShell の構文チェックは対象外です。

### C# の IDE 診断

`.editorconfig` に明示したルールをエラーとして扱います。
`Directory.Build.props` でビルド時にも有効にし、CI では formatter の検証とビルドの両方を実行します。
SDK の更新で新しい提案が加わっても、すべての提案を一律にエラーにはしません。

| 診断 | 内容 |
| --- | --- |
| IDE0001〜IDE0005 | 冗長な名前・メンバー参照・キャスト・using の削除 |
| IDE0007 | ローカル変数に `var` を使用 |
| IDE0011 | 条件分岐・ループに波括弧を付ける |
| IDE0017・IDE0028・IDE0090 | オブジェクト・コレクション初期化と `new` の簡略化 |
| IDE0029〜IDE0031・IDE0041 | null 処理の簡略化 |
| IDE0040・IDE0044 | アクセス修飾子の明示・変更しないフィールドの readonly 化 |
| IDE0055・IDE0059 | C# の書式・不要な代入 |
| IDE0063・IDE0065・IDE0161 | using 宣言・using の配置・ファイルスコープ名前空間 |

ローカルでの検証:

```bash
mise exec -- dotnet restore BackgroundServiceSample.slnx
mise exec -- dotnet format whitespace BackgroundServiceSample.slnx --no-restore --verify-no-changes
mise exec -- dotnet format style BackgroundServiceSample.slnx --no-restore --verify-no-changes --severity warn
mise exec -- dotnet build BackgroundServiceSample.slnx --no-restore
```

自動修正する場合は、上記 `dotnet format` の `--verify-no-changes` を外して実行してください。
変更後は差分とテスト結果を確認します。修正できない診断は手動で修正してください。
IDE0005 のビルド検証のため XML ドキュメント生成を有効にしていますが、
XML コメントの記述を必須にはしていません。

### 共通書式のローカルチェック

ローカルでは [editorconfig-checker v4.0.2](https://github.com/editorconfig-checker/editorconfig-checker/releases/tag/v4.0.2)
をインストールし、リポジトリ直下で次を実行します。CI とローカルで同じバージョン・設定を使います。

```bash
editorconfig-checker
```

チェッカーはファイルを自動修正しません。指摘された行を修正するか、EditorConfig 対応エディタで整形してから再実行してください。

[Windows E2E](https://github.com/Wataru-Toriumi/Background-Service-Sample/actions/workflows/windows-e2e.yml)
は GitHub-hosted の `windows-2022` runner で実行します。
ローカルと同じ `scripts/test-e2e.ps1` を使い、アプリの発行から FlaUI テストまで行います。
設定の保存と入力チェックのテストも実行します。

- `main` への push と Pull Request で自動実行
- Actions の **Windows E2E → Run workflow** から手動実行
- 成功・失敗にかかわらず、生成されたテスト結果を `windows-e2e-results-*` artifact として 14 日間保存

失敗時は実行画面の **Artifacts** から TRX、例外・プロセスログ、取得できた画面画像・UI 要素一覧を
ダウンロードしてください。セットアップやビルド段階で失敗した場合は、各ステップのログを確認します。
ジョブの制限時間は 15 分、テストのハング検知は 2 分です。
SDK バージョンを変更する場合は `global.json` と `mise.toml` を合わせて更新してください。

## 仕組み

```
[UI] StatusViewModel ──Start/Stop──▶ WorkerCoordinator ◀──参照── PeriodicTaskService [BackgroundService]
            ▲                                  │
            └────── 進捗 / ログ / 状態通知 ──────┘
```

`PeriodicTaskService` と `StatusViewModel` は互いを直接参照せず、
singleton の `WorkerCoordinator` をハブにして疎結合に連携します。
サービス側のスレッドから発生したイベントは `Dispatcher.UIThread` 経由で
UI スレッドにマーシャリングしてから反映します。
