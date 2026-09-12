IMAGE := livisor-server

.PHONY: server/run docker/server/build docker/server/run dotnet/test

server/run:
	dotnet run --project Livisor.Server

docker/server/build:
	docker build -t $(IMAGE) .

docker/server/run: docker/server/build
	docker run --rm -p 5210:8080 $(IMAGE)

dotnet/test:
	dotnet test

# Livisor.Shared を Client の埋め込みパッケージ（Packages/com.livisor.shared.unity）へ複製する。
# Client は Shared をサブモジュール参照ではなく複製で持つため、Shared を変えたらこれを実行して両方をそろえる。
# .meta は Client 側のものを保ち、新しいファイルの .meta は Unity が生成する。
SHARED_EMBED := Livisor.Client/Packages/com.livisor.shared.unity
.PHONY: shared/sync
shared/sync:
	rsync -a --delete --exclude '*.meta' --exclude 'bin/' --exclude 'obj/' --exclude '.artifacts/' \
	  --exclude '*.csproj' --exclude 'Directory.Build.*' --exclude 'package.json' --exclude '*.asmdef' \
	  Livisor.Shared/ $(SHARED_EMBED)/
