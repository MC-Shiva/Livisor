# 各ステージ内で再宣言必須
ARG APP_NAME=Livisor

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG APP_NAME
WORKDIR /src

# 依存関係の定義ファイルだけを先にコピーしてrestoreする
COPY ${APP_NAME}.Server/${APP_NAME}.Server.csproj ${APP_NAME}.Server/
COPY ${APP_NAME}.Shared/${APP_NAME}.Shared.csproj \
     ${APP_NAME}.Shared/Directory.Build.props \
     ${APP_NAME}.Shared/Directory.Build.targets \
     ${APP_NAME}.Shared/
RUN dotnet restore ${APP_NAME}.Server/${APP_NAME}.Server.csproj

COPY ${APP_NAME}.Server/ ${APP_NAME}.Server/
COPY ${APP_NAME}.Shared/ ${APP_NAME}.Shared/
# Release構成でビルドし
# 上でrestore済みのため二重実行しない
RUN dotnet publish ${APP_NAME}.Server/${APP_NAME}.Server.csproj \
    -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080

# dll名は直書き
ENTRYPOINT ["dotnet", "Livisor.Server.dll"]
