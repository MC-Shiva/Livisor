## 起動・テストの方法

リポジトリのルートで Makefile のコマンドを実行します。

## サーバーの起動

### .NET で起動

.NET SDK 10 が必要です。

```sh
make server/run
```

接続先は `http://localhost:5136` です。
Client から接続する場合は、接続先もこのポートに合わせます。

### Docker で起動

Docker を起動してから実行します。

```sh
make docker/server/run
```

`Dockerfile` からイメージをビルドし、コンテナを起動します。
接続先は `http://localhost:5210` です。コンテナ内の `8080` 番に転送します。

イメージのビルドだけを行う場合は、次を実行します。

```sh
make docker/server/build
```

## サーバーの停止

どちらの起動方法もターミナルで前面実行します。`Ctrl+C` で停止します。
Docker のコンテナは停止時に削除されます。イメージは残ります。
どちらもプロセス終了後や OS 再起動後の自動起動は行いません。

## テスト

```sh
make dotnet/test
```
