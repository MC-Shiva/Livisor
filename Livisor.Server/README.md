## Livisor.Server
Livisorにおけるサーバー実装をおくリポジトリ

## 実装について

- [実装の規則について](./Docs/Rules)
- [起動・テストの方法](./Docs/make.md)

## 演出キュー

Serverは起動時に`Livisor.Shared/DTO/DefaultActionSet.cs`を読み、roomごとのデフォルト演出に使います。
Adminの`ScheduleActionsAsync`は全行を検証してから追加予約へ加えます。
`TransportState.Actions`には、デフォルト演出と追加予約を時刻順にまとめた一覧を配信します。
同じ時刻ではデフォルト演出、追加予約の順です。

- `TimelineAction.Time`は曲の先頭からの位置です。指定位置で実行するのはClientです。
- CANCELは追加予約をすべて取り消します。デフォルト演出と再生状態は保ちます。
- STOPしてもキューは残ります。LiveSceneは曲を一時停止し、PLAYで続きから再開します。
- キューはメモリ上に保持します。Serverの再起動で追加予約は消え、デフォルト演出から始まります。

`Effect`の値は空白でない文字列として検証します。演出名は`EffectNames`の定数を使ってください。
DemoSceneは同じ定義をClientのSharedコピーから直接読み、Serverなしで動作します。

`TransportState`のKey 3は単一予約から配列に変更しています。ServerとClientを同時に更新してください。
Sharedの変更後は親リポジトリで`make shared/sync`を実行し、Clientを再ビルドします。

`dotnet test`でキューの追加・取消・並行更新とMessagePackの往復を検証します。
Adminの入力方法とUnityの疎通テストは、Clientの[通信ガイド](../Livisor.Client/Docs/server-communication.md)を参照してください。
