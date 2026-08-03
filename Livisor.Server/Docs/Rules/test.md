## テストについて
Livisor.ServerのテストはLivisor.Server.Testsで執筆する。

## 使用ライブラリ
- **xUnit** : テストフレームワーク
- **NSubstitute** : モックライブラリ

## ディレクトリ構成
層ごとにテストファイルを分ける。

```
Livisor.Server.Tests/
├── Domain/          # エンティティ・値オブジェクトの単体テスト（モック不要）
├── Application/     # UseCaseの単体テスト（依存はモック）
└── Infrastructure/  # 実装クラスの直接テスト
```

## テストの書き方
単体テストでは、テスト名を`メソッド名_前提条件_期待値`で書く

> https://osherove.com/blog/2005/4/3/naming-standards-for-unit-tests.html

複数操作をまとめて検証する場合は `クラス名_BasicOperations` など意図を表す名前にする。

## モック（NSubstitute）
Application層のUseCaseテストでは依存インターフェースをモックする。

```csharp
var cache = Substitute.For<ITimelineCache>();

// スタブ（戻り値の設定）
cache.GetAll("room1").Returns([t1, t2]);

// 呼び出し検証
cache.Received(1).Add("room1", timeline);
```

## テスト出力
Infrastructure層など状態確認が必要な場合は`ITestOutputHelper`でログ出力できる。

```csharp
public class TimelineCacheTests(ITestOutputHelper output)
{
    // output.WriteLine("...");
}
```

## 各層のテスト方針
| 層 | 方針 |
|---|---|
| Domain | モック不要。純粋ロジック（値オブジェクト・エンティティ）のみ検証 |
| Application | 依存（`ITimelineCache`等）はNSubstituteでモック |
| Infrastructure | 実装クラスを直接インスタンス化して検証 |
