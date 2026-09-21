# Background Service Sample

.NET Generic Host の `BackgroundService` を GUI アプリに統合するサンプルです。
`PeriodicTimer` で定期実行されるバックグラウンドタスクの進捗とログを、
UI（進捗バー・ログ一覧）にリアルタイム反映します。

GUI には WPF ではなく [Avalonia UI](https://avaloniaui.net/) を採用しているため、
**Windows / macOS / Linux** のいずれでも動作します。

## 必要環境

- .NET SDK 10.0.300（`mise.toml` で固定）

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
