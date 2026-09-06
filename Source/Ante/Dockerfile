# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble

WORKDIR /app

EXPOSE 8080

# ./out is a portable (RID-less) `dotnet publish` output produced by CI and copied in - no
# architecture-specific binaries, nothing compiled inside Docker. The base image's shared runtime
# executes the portable assemblies via `dotnet Ante.dll`.
#
# Build context is Source/Ante (not the repo root) - matching Cratis/Stagehand's Dockerfile - so
# `out` is relative to this directory, not the repo root. Building with a repo-root context would
# also work syntactically but drags the whole monorepo (node_modules, .git) into the build context
# for no reason.
COPY out ./

RUN rm -f appsettings.Development.json

ENTRYPOINT ["dotnet", "Ante.dll"]
