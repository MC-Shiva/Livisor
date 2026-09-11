## Livisor.Server
Livisorにおけるサーバー実装をおくリポジトリ

## 実装について

- [実装の規則について](./Docs/Rules)
- [起動・テストの方法](./Docs/make.md)

## 演出の予約

Adminから`TimelineAction`を受け取り、同じroomのClientへ配信します。
`ActionType.Effect`の`Value`は演出名の文字列です。LiveSceneでは`Time`を曲の先頭からの位置として扱います。

- `ScheduledAction`に最大1件保持します。新しい予約で置き換え、取消でnullにします。
- 時刻と値の型を検証し、`TransportState`で再生状態と予約を配信します。
- 指定時刻に演出を実行するのはClientです。

サーバーの`Effect`検証は、値が空白ではない文字列であることを確認します。
演出名は`EffectNames`の定数を使ってください。実際に再生するのはClientです。
`Livisor.Shared/DTO/DefaultActionSet.cs`はDemoSceneが直接読む事前定義です。
Serverはこの定義を読み込まず、配信にも含めません。DemoSceneはServerなしで動作します。

確認には親リポジトリで`dotnet test`を実行します。
`DefaultActionSetTests`が定義、`TransportMapperTests`が配信内容、`TransportStateSerializationTests`がMessagePackの互換性を検証します。
Adminの入力方法とUnityの疎通テストは、Clientの[通信ガイド](../Livisor.Client/Docs/server-communication.md)を参照してください。
