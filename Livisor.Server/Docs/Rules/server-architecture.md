# Livisor.Server アーキテクチャ

再生トランスポート（再生・停止・予約）と状態同期（心拍数・音量など）を room 単位で扱うサーバー。責務を内側（業務ルール）から外側（通信・技術詳細）へ分離したレイヤードアーキテクチャ（Clean Architecture 準拠）を採用し、依存は常に内側の `Domain` へ向く。

通信は用途で経路を分ける。一度きりの操作は Unary サービス、変化し続ける値の同期は StreamingHub が担う。Unary はサーバーからクライアントへ押し出せないため、Unary で確定した結果は StreamingHub の配信グループ経由で同じ room の参加者へ届ける。

## ディレクトリ構成
```
Livisor.Server/
├── Domain/          … 業務ルールと不変条件
│   ├── Entity/      …   エンティティ・集約
│   ├── ValueObject/ …   値オブジェクト
│   ├── Cache/       …   キャッシュの抽象(インターフェースを配置)
│   └── Time/        …   現在時刻取得の抽象(インターフェースを配置)
├── Application/     … ユースケース
│   └── UseCases/
├── Infrastructure/  … 技術詳細の実装(Domainに配置しているInterfaceの実装)
├── Logging/         … ロギングの構成と、構造化フィールドを渡すための拡張
└── Presentation/    … 通信境界
    ├── Hubs/          … StreamingHub
    ├── Mapping/       … DTO ↔ Domain 変換
    ├── Providers/     … 配信グループなど、Hub と Unary サービスで共有する部品
    └── UnaryServices/ … Unary サービス
```

## 各層の役割

### Domain
ビジネスルールやロジック、値オブジェクトを表現する。
他のどの層にも依存しない。
保持や永続化などの「抽象」もこの層で定義し、実装は外側の層に委ねる（依存性逆転の法則）。

現在時刻（`Time/IClock`）もこの層に置く。サーバー時刻は再生開始の基準そのもので、
予約アクションがいつ発火するかを決める業務上の値だからである。
技術詳細として外側に隠さず、Domain が定義した抽象として内側から扱う。

### Application
ユースケース（業務フロー）を調停する。
Domain が定義したinterfaceを介して「取得・保存」などの流れを組み立てる。
通信や永続化の具体は知らない。

### Infrastructure
Domain で定義したinterfaceの具体実装を置く（保持・永続化などの技術詳細）。
技術詳細をこの層に閉じ込め、内側から隠蔽する。


### Presentation
クライアントとの通信境界。
リクエストの受け口、DTO ↔ Domain の変換、通信レイヤーのエラー変換を担う。

---
依存の向き: **Presentation / Infrastructure → Application → Domain**（Domain は他層に依存しない）。

例外として、Domain は `Livisor.Shared.Common`（`ActionType` / `ActionValue` / `PlaybackTime`）だけを参照する。
これらはサーバーとクライアントで意味を揃える必要がある共通語彙で、Domain より内側に置く扱いとする。
`Livisor.Shared.DTO`（ワイヤ形式）と `Livisor.Shared.Hubs` / `UnaryServices`（通信契約）は Domain から参照しない。
DTO と Domain の変換は Presentation/Mapping に閉じる。
