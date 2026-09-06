# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble

WORKDIR /app

EXPOSE 8080

# ./out is a portable (RID-less) `dotnet publish` output produced by CI and copied in - no
# architecture-specific binaries, nothing compiled inside Docker. The base image's shared runtime
# executes the portable assemblies via `dotnet Ante.dll`.
COPY Source/Ante/out ./

RUN rm -f appsettings.Development.json

ENTRYPOINT ["dotnet", "Ante.dll"]
