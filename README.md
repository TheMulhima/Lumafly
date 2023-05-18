# Scarab+
![build](https://github.com/TheMulhima/Scarab/actions/workflows/dotnet.yml/badge.svg)
![GitHub all releases](https://img.shields.io/github/downloads/TheMulhima/Scarab/total)

This is a fork of Scarab, a mod manager for Hollow Knight. Why does this exist? Because we felt like there were some 
major features missing from the primary Hollow Knight mod manager that everyone uses.  

This fork has everything that Scarab has plus a few nice features:
- Improved search options (filter by tags, by authors, search by dependency).
- Allows shareable links to run commands in scarab (for more info read [this](https://github.com/TheMulhima/Scarab#scarab-url-scheme))
- Filter for newly released/updated mods.
- An offline mode.
- Pin and manually install mods.
- Important bug fixes for
  - Mods not downloading correctly
  - Showing that Modding API is disabled when it is not.
- And other minor things (open mod's settings file, handling manually installed mods, ability to disable/uninstall all, load mod list from custom modlist)

see [this](https://github.com/TheMulhima/Scarab/blob/master/CHANGELOG.md) for a full changelog

## Scarab URL Scheme
Scarab now uses Windows/Mac/Linux URL Scheme, this means shareable links can now open and run commands on scarab. You can use the [commands website](https://themulhima.github.io/Scarab/commands) to get correctly formatted links to share. There are 4 commands as of v2.0.0.0
- [Download](https://themulhima.github.io/Scarab/commands/download) - Open scarab and download mod(s).
- [Custom Modlinks](https://themulhima.github.io/Scarab/commands/customModLinks) - Open Scarab and load its mod list from a custom modlinks
- [Reset](https://themulhima.github.io/Scarab/commands/reset) - Open Scarab and reset its persistent settings.
- [Force Update All](https://themulhima.github.io/Scarab/commands/forceUpdateAll) - Open Scarab and reinstalls all mods.Could help fix issues with mods

There is also the [redirect link](https://themulhima.github.io/Scarab/redirect) which can be used to link any scarab command.
## Usage
- Get the latest release [here](https://github.com/TheMulhima/Scarab/releases/latest).
- Unzip and run it.
- Mods appear in the top left corner of the game title screen after installation.

## What Scarab+ Looks Like
![screenshot](https://raw.githubusercontent.com/TheMulhima/Scarab/static-resources/Readme%20Assets/Default.png)

## Many More Options
![allflyouts](https://raw.githubusercontent.com/TheMulhima/Scarab/static-resources/Readme%20Assets/All%20Flyouts.png)

## Whats New Tab
![whatsnew](https://raw.githubusercontent.com/TheMulhima/Scarab/static-resources/Readme%20Assets/whatsnew.png)

## More Compact UI
![expanded](https://raw.githubusercontent.com/TheMulhima/Scarab/static-resources/Readme%20Assets/expanded.png)

## Contributions
- Contributions are open. You can see what features are currenly requested over [here](https://github.com/TheMulhima/Scarab/labels/enhancement)

"This is the best version of scarab rn" - Dandy
