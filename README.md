# Scarab+
![build](https://github.com/TheMulhima/Scarab/actions/workflows/dotnet.yml/badge.svg)
![GitHub all releases](https://img.shields.io/github/downloads/TheMulhima/Scarab/total)

This is a fork of Scarab, a mod manager for Hollow Knight. Why does this exist? Because we felt like there were some 
major features missing from the primary Hollow Knight mod manager that everyone uses.  

This fork has everything that Scarab has plus a few nice features:
- Improved search options (filter by tags, by authors, search by dependency).
- Allows shareable links to run commands in scarab (for more info read the [url scheme explanation](https://github.com/TheMulhima/Scarab#scarab-url-scheme))
- Filter for newly released/updated mods.
- An offline mode.
- Pin and manually install mods.
- Better error messages and better error handling.
- Important bug fixes for
  - Mods not downloading correctly.
  - Detecting and toggling Modding API when vanilla version is not found/present
- And other minor things (open mod's settings file, handling manually installed mods, ability to disable/uninstall all, load mod list from custom modlist)

see [changelog](https://github.com/TheMulhima/Scarab/blob/master/CHANGELOG.md) for full list of changes.

## Usage
- Download the latest version from [https://themulhima.github.io/Scarab?download](https://themulhima.github.io/Scarab?download).
- Unzip and run it.
- Mods appear in the top left corner of the game title screen after installation.

## Scarab URL Scheme
Scarab+ uses Windows/Mac/Linux URL Scheme and in combination with scarab's website, it will allow shareable links to 
open and run commands on scarab. You can use the [commands website](https://themulhima.github.io/Scarab/commands) to get correctly formatted links to share.  

The main purpose is to share downloadable links of mods using the [download](https://themulhima.github.io/Scarab/commands/download) command. 
Sharing a link such as [https://themulhima.github.io/Scarab/commands/download?mod=Satchel](https://themulhima.github.io/Scarab/commands/download?mod=Satchel)
will open Scarab and download the mod Satchel. The button on the command page can help correctly format the link. 

There are 4 other shareable links via the website as well:
- [Custom Modlinks](https://themulhima.github.io/Scarab/commands/customModLinks) - Open Scarab and load its mod list from a custom modlinks
- [Reset](https://themulhima.github.io/Scarab/commands/reset) - Open Scarab and reset its persistent settings.
- [Force Update All](https://themulhima.github.io/Scarab/commands/forceUpdateAll) - Open Scarab and reinstalls all mods.Could help fix issues with mods
- [Redirect link](https://themulhima.github.io/Scarab/redirect) which can be used to link any scarab command.

## Contributions
Contributions are open. You can see what features are currently requested over [here](https://github.com/TheMulhima/Scarab/labels/enhancement)
#### Open Issues
- [#29](https://github.com/TheMulhima/Scarab/issues/29) - Translate Scarab to more languages

## Screenshot: What Scarab+ Looks Like
![screenshot](https://raw.githubusercontent.com/TheMulhima/Scarab/static-resources/Readme%20Assets/Default.png)

## Screenshot: Many More Options
![allflyouts](https://raw.githubusercontent.com/TheMulhima/Scarab/static-resources/Readme%20Assets/All%20Flyouts.png)

## Screenshot: Whats New Tab
![whatsnew](https://raw.githubusercontent.com/TheMulhima/Scarab/static-resources/Readme%20Assets/whatsnew.png)

## Credits
- [56](https://github.com/fifty-six): Original creator of Scarab

#### Translations Credits
- [Clazex](https://github.com/Clazex) - Chinese translations
- [luiz_eldorado](https://github.com/luizeldorado) - Portuguese translations
- [Dastan](https://github.com/Dastan21) - French translations

## Testimonials
"This is the best version of scarab rn" - Dandy
