#!/bin/bash

version=$(grep -oP -m 1 '\* \K[0-9]*\.[0-9]*\.[0-9]*\.[0-9]*' docs/ReleaseNotes.md)
echo Version=$version

dotnet publish -r linux-x64 -c Release -f net6.0 -p:AssemblyVersion=$version --self-contained

