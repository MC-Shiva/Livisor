## Makeコマンドについて

## サーバの起動方法

- dotnetで起動
```
make server/run
```

- イメージをビルド
```
make docker/server/build
```

- イメージをビルドした上で起動
```
make docker/server/run
```

## テスト方法
```
dotnet test
```
