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
