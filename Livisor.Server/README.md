## Livisor.Server
Livisorにおけるサーバー実装をおくリポジトリ

## 実装について

- [実装の規則について](./Docs/Rules)
- [起動・テストの方法](./Docs/make.md)

## デフォルト演出と予約

演出の定義は`Livisor.Shared/DTO/DefaultActionSet.cs`にあるC#の`TimelineAction[]`です。
`Time`は曲の先頭からの位置、`ActionType.Effect`の`Value`は演出名の文字列です。

- 起動時に`Program.cs`が各行を`ScheduledActionMapper.ToDomain`で検証します。不正な時刻や値の型なら起動に失敗します。
- 配信時に`TransportMapper`が定義を`TransportState.DefaultActions`へ入れます。サーバー自身が指定時刻に演出を実行する処理はありません。
- Adminからの予約は`ScheduledAction`に最大1件保持します。予約の登録・取消で`DefaultActions`は変わりません。

サーバーの`Effect`検証は、値が空白ではない文字列であることを確認します。
演出名は`EffectNames`の定数を使ってください。実際に再生するのはClientです。
定義を変更したら親リポジトリで`make shared/sync`を実行し、Serverを再ビルド・再起動してください。

確認には親リポジトリで`dotnet test`を実行します。
`DefaultActionSetTests`が定義、`TransportMapperTests`が配信内容、`TransportStateSerializationTests`がMessagePackの互換性を検証します。
Clientでの演出再生まで確認済みの経路はDemoSceneです。Live・Adminを通した演出の連携は作成途中です。
