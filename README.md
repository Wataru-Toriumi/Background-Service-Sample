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

ウィンドウが開いたら **開始** を押すと、3 秒間隔でタスクが実行され、
進捗バーが 0 → 100% に進みます。**停止** で待機状態に戻ります。

## 構成

Avalonia アプリと UI に依存しない Workers ライブラリを別プロジェクトに分けています。
アプリが Workers を参照し、Generic Host の起動・終了と DI 登録はアプリ側で管理します。

| ファイル | 役割 |
| --- | --- |
| `src/BackgroundServiceSample/Program.cs` | Generic Host を起動し、DI コンテナを Avalonia に橋渡しするエントリポイント |
| `src/BackgroundServiceSample.Workers/PeriodicTaskService.cs` | `PeriodicTimer` で定期起動する `BackgroundService`。アクティブ時のみ擬似タスクを実行 |
| `src/BackgroundServiceSample.Workers/WorkerCoordinator.cs` | UI とサービス間で開始/停止の制御・進捗・ログを仲介する singleton |
| `src/BackgroundServiceSample/ViewModels/MainWindowViewModel.cs` | サービスのイベントを UI スレッドへマーシャリングし、状態・進捗・ログに反映 |
| `src/BackgroundServiceSample/Views/MainWindow.axaml` | 開始/停止ボタン、進捗バー、ログ一覧を持つメイン画面 |

アプリと Workers をまとめてビルドするには、次を実行します。

```bash
mise exec -- dotnet build src/BackgroundServiceSample
```

## Windows E2E テスト

`tests/BackgroundServiceSample.E2E` は xUnit と FlaUI.UIA3 を使い、公開した実際のアプリを
別プロセスとして起動します。バックグラウンドサービスも実物を動かします。

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

要素は `AutomationProperties.AutomationId` で検索し、状態変化は最大 20 秒待機します。
テストは並列実行しません。結果は `artifacts/e2e/results` に保存します。
失敗時にはプロセスログと例外、取得可能な場合はウィンドウ画像と UI 要素一覧も保存します。

macOS / Linux では FlaUI の E2E は実行できません。
CI に組み込む場合も、GUI 操作できる Windows セッションが必要です。

## GitHub Actions

[Windows E2E](https://github.com/Wataru-Toriumi/Background-Service-Sample/actions/workflows/windows-e2e.yml)
は GitHub-hosted の `windows-2022` runner で実行します。
ローカルと同じ `scripts/test-e2e.ps1` を使い、アプリの発行から FlaUI テストまで行います。

- `main` への push と Pull Request で自動実行
- Actions の **Windows E2E → Run workflow** から手動実行
- 成功・失敗にかかわらず、生成されたテスト結果を `windows-e2e-results-*` artifact として 14 日間保存

失敗時は実行画面の **Artifacts** から TRX、例外・プロセスログ、取得できた画面画像・UI 要素一覧を
ダウンロードしてください。セットアップやビルド段階で失敗した場合は、各ステップのログを確認します。
ジョブの制限時間は 15 分、テストのハング検知は 2 分です。
SDK バージョンを変更する場合は `global.json` と `mise.toml` を合わせて更新してください。

## 仕組み

```
[UI] MainWindowViewModel ──Start/Stop──▶ WorkerCoordinator ◀──参照── PeriodicTaskService [BackgroundService]
            ▲                                  │
            └────── 進捗 / ログ / 状態通知 ──────┘
```

`PeriodicTaskService` と `MainWindowViewModel` は互いを直接参照せず、
singleton の `WorkerCoordinator` をハブにして疎結合に連携します。
サービス側のスレッドから発生したイベントは `Dispatcher.UIThread` 経由で
UI スレッドにマーシャリングしてから反映します。
