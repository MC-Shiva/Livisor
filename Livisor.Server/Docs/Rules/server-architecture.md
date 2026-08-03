# Livisor.Server アーキテクチャ

タイムライン配信サーバー。責務を内側（業務ルール）から外側（通信・技術詳細）へ分離したレイヤードアーキテクチャ（Clean Architecture 準拠）を採用し、依存は常に内側の `Domain` へ向く。

## ディレクトリ構成
```
Livisor.Server/
├── Domain/          … 業務ルールと不変条件
│   ├── Entity/      …   エンティティ・集約
│   ├── ValueObject/ …   値オブジェクト
│   └── Cache/       …   キャッシュの抽象(インターフェースを配置)
├── Application/     … ユースケース
│   └── UseCases/
├── Infrastructure/  … 技術詳細の実装(Domainに配置しているInterfaceの実装)
└── Presentation/    … 通信境界
    ├── Hubs/          … StreamingHub
    ├── Mapping/       … DTO ↔ Domain 変換
    └── UnaryServices/ … Unary サービス
```

## 各層の役割

### Domain
ビジネスルールやロジック、値オブジェクトを表現する。
他のどの層にも依存しない。
保持や永続化などの「抽象」もこの層で定義し、実装は外側の層に委ねる（依存性逆転の法則）。

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
