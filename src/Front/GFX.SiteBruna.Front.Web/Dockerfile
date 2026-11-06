# Imagem do Front.Web (docs/DEPLOY.md). Contexto = raiz do repositório:
#   docker build -f src/Front/GFX.SiteBruna.Front.Web/Dockerfile -t bruna-front:local .
# O CSS gerado já vem no repositório: a imagem não precisa de Node.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/Front/GFX.SiteBruna.Front.Web/ src/Front/GFX.SiteBruna.Front.Web/
RUN dotnet publish src/Front/GFX.SiteBruna.Front.Web/GFX.SiteBruna.Front.Web.csproj -c Release -o /app/publish /p:UseAppHost=false

# Imagem comum (não a chiseled): traz ICU, necessária para o pt-BR
FROM mcr.microsoft.com/dotnet/aspnet:10.0
# tzdata: hora local (TZ=America/Sao_Paulo no compose).
# /dados/chaves e /dados/fotos: os volumes herdam o dono da pasta na primeira subida.
RUN apt-get update \
    && apt-get install -y --no-install-recommends tzdata \
    && rm -rf /var/lib/apt/lists/* \
    && mkdir -p /dados/chaves /dados/fotos \
    && chown -R app:app /dados
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER app
ENTRYPOINT ["dotnet", "GFX.SiteBruna.Front.Web.dll"]
