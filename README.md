# Scarab+
![build](https://github.com/TheMulhima/Scarab/actions/workflows/build.yml/badge.svg)
![test](https://github.com/TheMulhima/Scarab/actions/workflows/test.yml/badge.svg)
[![website](https://img.shields.io/website?down_color=red&down_message=offline&up_color=green&up_message=online&url=https%3A%2F%2Fthemulhima.github.io%2FScarab)](https://themulhima.github.io/Scarab)  
[![GitHub all releases](https://img.shields.io/github/downloads/TheMulhima/Scarab/total)](https://github.com/TheMulhima/Scarab/releases)
[![contributors](https://img.shields.io/github/contributors/TheMulhima/Scarab)](https://github.com/TheMulhima/Scarab/issues)  
[![discord](https://img.shields.io/discord/879125729936298015?label=discord)](https://discord.gg/VDsg3HmWuB)

This is a cross platform mod manager for [Hollow Knight](https://www.hollowknight.com). 

This specific version of Scarab, Scarab+ is a fork of the "normal" Scarab. It is an updated version of Scarab that exists because there were some
major features missing from Scarab that we wanted to add.
See [changelog](https://github.com/TheMulhima/Scarab/blob/master/CHANGELOG.md) for the full list of them.

## Usage
- Download the latest version from the [automatic download link](https://themulhima.github.io/Scarab?download) or the [releases page](https://github.com/TheMulhima/Scarab/releases/latest).
- Search through and download the mods you like.
- Mods appear in the top left corner of the game title screen after installation.
- Enable/Disable mods from affecting the game using the toggle and update outdated mods using the orange update button. 
- If you are unable to connect to the internet, Scarab can be launched in offline mode where you can toggle mods/api.

## Screenshot: What Scarab+ looks like
![demo](https://github.com/TheMulhima/Scarab/blob/static-resources/Readme%20Assets/ModList.png?raw=true)

## Features
- Automatically downloads the [Modding API](https://github.com/hk-modding/api) which is required for mods to load. It also allows switching between modded and vanilla via the Toggle API button.
- Loads its modlist from the [official modlinks](https://github.com/hk-modding/modlinks) but that can be changed by providing a custom modlinks URI in the settings tab or via a [command](#commands).
- Search through the 300+ mods available and narrow down the search by the mod's tags and authors. Also allows searching through mod descriptions and [searching by dependency](#search-by-dependency).
- Display mods that were updated or newly released recently (within the last week/month). 
- For mods not in official modlinks, scarab provides a manually install button which can correctly install  it. 
- Pin mods by right clicking on a mod to ensure scarab never deletes it.
- Share links that run commands in scarab see [commands](#commands) for more info.
- Open a mod's global settings file to edit its settings.

## Features Explanation
Here are a list of explanations of features that require more information.
### Commands
Scarab+ allows you to use shareable links to open and run commands in scarab. You can use the [commands website](https://themulhima.github.io/Scarab/commands) to get the correctly formatted links.  

#### General Commands
The main shareable commands that anyone can use.
- [Download Mod](https://themulhima.github.io/Scarab/commands/download): Share links to open Scarab and download mods using this command. Sharing a link such as [this](https://themulhima.github.io/Scarab/commands/download?mods=Satchel).
will open Scarab and download the mod "Satchel".
- [Custom Modlinks](https://themulhima.github.io/Scarab/commands/customModLinks) - Open Scarab and load its mod list from a custom modlinks.

#### Support Commands
Commands that can be used to help fix problems with Scarab or Hollow Knight Mods
- [Reset](https://themulhima.github.io/Scarab/commands/reset) - Opens Scarab and reset its persistent settings. Equivalent to downloading and opening Scarab for the first time.
- [Force Update All](https://themulhima.github.io/Scarab/commands/forceUpdateAll) - Opens Scarab and reinstalls all mods. Could help fix issues that happened because mods are not downloaded correctly.

#### Other Commands
- [Redirect link](https://themulhima.github.io/Scarab/redirect) You can use this link to redirect to any scarab command.

### Search by Dependency
Scarab allows you to search for mods that are dependent or integrated with a specific mod. For example if you want to see Randomizer 4 connections you can search by dependency of randomizer 4 and you will see mods like RandoMapMod, RandoPlus, and BenchRando.

### Settings
There are currently 4 settings that can be changed in Scarab
- Automatically remove unused dependencies (Default: No) - Do you want scarab to remove mods that are listed as dependencies of mods that are now being uninstalled.
- Warn before removing a dependent mod (Default: Yes) - Do you want scarab to warn before disabling or uninstalling a mod that is listed as a required dependency of an installed and enabled mod which could cause it to not function correctly.
- Use custom modlinks (Default: No) - Do you want Scarab to load its mod list from another source other than the [official modlinks](https://github.com/hk-modding/modlinks). Enabling this will give you the prompt for the custom modlinks URI.
- Game Path - Which install of the game do you want Scarab to modify.

### Tools for mod developers
- You can mark a mod as a custom build of the mod by right clicking the mod and clicking on "Register as not in modlinks"
- Pinning a not in modlinks mod ensures Scarab will never automatically replace it.
- To test if you mod installs correctly, create a fork of [modlinks](https://github.com/hk-modding/modlinks) and in your fork add your mod's manifest in ModLinks.xml. Then open Scarab and in the settings tab give Scarab the link to the ModLinks.xml and reload Scarab.

## Contributions
If you want to suggest a feature or report a bug, report it on the [issues page](https://github.com/TheMulhima/Scarab/issues/new/choose).  
If you want to contribute to Scarab, feel free to. You can see what features are currently requested over [here](https://github.com/TheMulhima/Scarab/labels/enhancement)
#### Some open issues you can help with
- [#29](https://github.com/TheMulhima/Scarab/issues/29) - Translate Scarab to more languages

## Credits
- [56](https://github.com/fifty-six): Original creator of Scarab

#### Translations Credits
- [Clazex](https://github.com/Clazex) - Chinese translations
- [luiz_eldorado](https://github.com/luizeldorado) - Portuguese translations
- [Dastan](https://github.com/Dastan21) - French translations
