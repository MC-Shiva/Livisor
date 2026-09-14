## Livisor.Server
Livisorにおけるサーバー実装をおくリポジトリ

## 実装について

- [実装の規則について](./Docs/Rules)
- [起動・テストの方法](./Docs/make.md)

## 演出キュー

Serverは起動時に`Livisor.Shared/Common/DefaultTimeline.cs`を読み、roomごとのデフォルト演出に使います。
Adminの`ScheduleActionsAsync`は全行を検証してから追加予約へ加えます。
`TransportState.Actions`には、デフォルト演出と追加予約を時刻順にまとめた一覧を配信します。
同じ時刻ではデフォルト演出、追加予約の順です。

- `TimelineAction.Time`は曲の先頭からの位置です。指定位置で実行するのはClientです。
- CANCELは追加予約をすべて取り消します。デフォルト演出と再生状態は保ちます。
- STOPしてもキューは残ります。LiveSceneは曲を一時停止し、PLAYで続きから再開します。
- キューはメモリ上に保持します。Serverの再起動で追加予約は消え、デフォルト演出から始まります。

`Effect`の値は空白でない文字列として検証します。演出名は`EffectNames`の定数を使ってください。
DemoSceneは紙吹雪・銀テープをSharedコピーから、雷をClientのC#定義から読み、Serverなしで動作します。

`TransportState`のKey 3は単一予約から配列に変更しています。ServerとClientを同時に更新してください。
Sharedの変更後は親リポジトリで`make shared/sync`を実行し、Clientを再ビルドします。

`dotnet test`でキューの追加・取消・並行更新とMessagePackの往復を検証します。
Adminの入力方法とUnityの疎通テストは、Clientの[通信ガイド](../Livisor.Client/Docs/server-communication.md)を参照してください。

## 即時演出

`FireEffectAsync(roomId, EffectCommand)`は再生中のroomへ`OnEffectTriggered`で通知します。
雷・銀テープ・紙吹雪の開始と停止に対応します。雷は`unity-chan`・`audience`・`stage`の対象名が必須です。
操作は予約キューや状態に保存せず、現在の参加者へ毎回1回配信します。再接続時には再送しません。
ServerとClientを同時に更新し、`make shared/sync`で通信契約を揃えてください。
