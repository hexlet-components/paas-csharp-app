# Render: Language Docker, Dockerfile Path ./Dockerfile, Docker Command пустая.
# Готовой среды .NET у Render нет; платформе передаём инструкцию сборки.
FROM mcr.microsoft.com/dotnet/sdk:11.0 AS build
WORKDIR /src
COPY Directory.Build.props .
COPY src/PaasCsharpApp/PaasCsharpApp.csproj src/PaasCsharpApp/
# Аудит пакетов и предупреждения как ошибки проверяет CI (mise run check),
# новая уязвимость в базе не должна ломать сборку образа на Render.
RUN dotnet restore src/PaasCsharpApp/PaasCsharpApp.csproj -p:NuGetAudit=false
COPY src/PaasCsharpApp src/PaasCsharpApp
RUN dotnet publish src/PaasCsharpApp/PaasCsharpApp.csproj -c Release --no-restore -p:TreatWarningsAsErrors=false -o /out

FROM mcr.microsoft.com/dotnet/aspnet:11.0
WORKDIR /app
# Образ по умолчанию слушает порт 8080; адрес и порт задаёт код по PORT.
ENV ASPNETCORE_HTTP_PORTS=
COPY --from=build /out .
EXPOSE 3000
CMD ["dotnet", "PaasCsharpApp.dll"]
