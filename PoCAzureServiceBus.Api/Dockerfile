FROM microsoft/dotnet:2.1-aspnetcore-runtime AS base
WORKDIR /app
EXPOSE 80

FROM microsoft/dotnet:2.1-sdk AS build
WORKDIR /src
COPY ["PoCAzureServiceBus.Api/PoCAzureServiceBus.Api.csproj", "PoCAzureServiceBus.Api/"]
RUN dotnet restore "PoCAzureServiceBus.Api/PoCAzureServiceBus.Api.csproj"
COPY . .
WORKDIR "/src/PoCAzureServiceBus.Api"
RUN dotnet build "PoCAzureServiceBus.Api.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "PoCAzureServiceBus.Api.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "PoCAzureServiceBus.Api.dll"]