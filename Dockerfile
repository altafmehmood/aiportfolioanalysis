# Build stage for Angular frontend
FROM node:22-alpine AS frontend-build
WORKDIR /app/clientapp
COPY AiPortfolioAnalysis.Web/ClientApp/package*.json ./
RUN npm ci
COPY AiPortfolioAnalysis.Web/ClientApp/ ./
RUN npm run build -- --configuration production

# Build stage for .NET backend
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS backend-build
WORKDIR /app
COPY *.sln ./
COPY AiPortfolioAnalysis.Web/*.csproj ./AiPortfolioAnalysis.Web/
RUN dotnet restore
COPY AiPortfolioAnalysis.Web/ ./AiPortfolioAnalysis.Web/
COPY --from=frontend-build /app/clientapp/dist ./AiPortfolioAnalysis.Web/wwwroot/
RUN dotnet publish AiPortfolioAnalysis.Web/AiPortfolioAnalysis.Web.csproj -c Release -o out /p:PublishRunWebpack=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS runtime
WORKDIR /app
COPY --from=backend-build /app/out .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "AiPortfolioAnalysis.Web.dll"]