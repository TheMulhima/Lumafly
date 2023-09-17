# Lumafly Auto Updater

This is the auto updater for Lumafly.

## How it works

The AU has Lumafly.exe in it as an embedded resource. When it is opened, it places the embedded exe into the same folder.
and opens it. If required it deleted the old exe before placing the new one.

## How to build

Make sure to place a Lumafly.exe (from dotnet publish of a windows version) next to the csproj and build it. There are no external dependencies
