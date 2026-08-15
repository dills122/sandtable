set dotenv-load := false

solution := "Sandtable.slnx"

default:
    @just --list

setup:
    dotnet --version
    dotnet restore {{ solution }}

restore:
    dotnet restore {{ solution }}

build: restore
    dotnet build {{ solution }} --no-restore

test: build
    dotnet test --solution {{ solution }} --no-build

format:
    dotnet format {{ solution }}

format-check: restore
    dotnet format {{ solution }} --verify-no-changes --no-restore

check: format-check build test

run:
    dotnet run --project src/Cna.AppHost/Cna.AppHost.csproj
