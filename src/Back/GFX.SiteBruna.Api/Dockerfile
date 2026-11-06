# Imagem da Api (docs/DEPLOY.md). Contexto = raiz do repositório:
#   docker build -f src/Back/GFX.SiteBruna.Api/Dockerfile -t bruna-api:local .
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/Back/ src/Back/
RUN dotnet publish src/Back/GFX.SiteBruna.Api/GFX.SiteBruna.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

# Imagem comum (não a chiseled): traz ICU, necessária para o pt-BR
FROM mcr.microsoft.com/dotnet/aspnet:10.0
# tzdata: hora local (TZ=America/Sao_Paulo no compose).
# libgssapi-krb5-2: o Npgsql procura a biblioteca do Kerberos ao conectar; sem ela, um erro inofensivo aparece no log.
# /dados/fotos: o volume herda o dono da pasta na primeira subida.
RUN apt-get update \
    && apt-get install -y --no-install-recommends tzdata libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/* \
    && mkdir -p /dados/fotos \
    && chown -R app:app /dados
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER app
ENTRYPOINT ["dotnet", "GFX.SiteBruna.Api.dll"]
