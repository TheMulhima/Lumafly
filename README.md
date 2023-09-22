# Lumafly

![build](https://github.com/TheMulhima/Lumafly/actions/workflows/build.yml/badge.svg)
![test](https://github.com/TheMulhima/Lumafly/actions/workflows/test.yml/badge.svg)
[![website](https://img.shields.io/website?down_color=red&down_message=offline&up_color=32c854&up_message=online&url=https%3A%2F%2Fthemulhima.github.io%2FLumafly)](https://themulhima.github.io/Lumafly)  
[![GitHub all releases](https://img.shields.io/github/downloads/TheMulhima/Lumafly/total)](https://github.com/TheMulhima/Lumafly/releases)
[![contributors](https://img.shields.io/github/contributors/TheMulhima/Lumafly)](https://github.com/TheMulhima/Lumafly/graphs/contributors)  
[![discord](https://img.shields.io/discord/879125729936298015?label=discord)](https://discord.gg/VDsg3HmWuB)

This is a cross platform mod manager for [Hollow Knight](https://www.hollowknight.com) which is fully localized in English, Spanish, Portuguese, French, Chinese, Russian and Polish.

Formerly known as **Scarab+**

## Usage

- Download the latest version from the [automatic download link](https://themulhima.github.io/Lumafly?download) or the [releases page](https://github.com/TheMulhima/Lumafly/releases/latest).
- Search through and download the mods you like.
- Mods appear in the top left corner of the game title screen after installation.
- Enable/Disable mods from affecting the game using the toggle and update outdated mods using the orange update button.
- If you are unable to connect to the internet, Lumafly can be launched in offline mode where you can toggle mods/api.

## Screenshot: What Lumafly looks like

![info](https://github.com/TheMulhima/Lumafly/blob/static-resources/Readme%20Assets/Info.png?raw=true)
![demo](https://github.com/TheMulhima/Lumafly/blob/static-resources/Readme%20Assets/ModList.png?raw=true)

## Features

- Automatically downloads the [Modding API](https://github.com/hk-modding/api) which is required for mods to load. It also allows switching between modded and vanilla via the Toggle API button.
- Loads its modlist from the [official modlinks](https://github.com/hk-modding/modlinks) but that can be changed by providing a custom modlinks URI in the settings tab or via a [command](#commands).
- Search through the 300+ mods available and narrow down the search by the mod's tags and authors. Also allows searching through mod descriptions and [searching by dependency](#search-by-dependency).
- Display mods that were updated or newly released recently (within the last week/month).
- For mods not in official modlinks, lumafly provides a manually install button which can correctly install  it.
- Pin mods by right clicking on a mod to ensure lumafly never deletes it.
- Share links that run commands in lumafly see [commands](#commands) for more info.
- Open a mod's global settings file to edit its settings.

If you want to see what features changed between versions please see the [changelog](https://github.com/TheMulhima/Lumafly/blob/master/CHANGELOG.md).

## Features Explanation

Here are a list of explanations of features that require more information.

### Commands

Lumafly allows you to use shareable links to open and run commands in lumafly. You can use the [commands website](https://themulhima.github.io/Lumafly/commands) to view and create the links.

#### General Commands

The main shareable commands that anyone can use.

- [Download Mod](https://themulhima.github.io/Lumafly/commands/download): Share links to open Lumafly and download mods using this command. Sharing a link such as [this](https://themulhima.github.io/Lumafly/commands/download?mods=Satchel) will open Lumafly and download the mod "Satchel".
- [Share modpack](https://themulhima.github.io/Lumafly/commands/modpack): Used to share a modpack link with others.

- [Custom Modlinks](https://themulhima.github.io/Lumafly/commands/customModLinks) - Open Lumafly and load its mod list from a custom modlinks.

- [Launch Game](https://themulhima.github.io/Lumafly/redirect?link=scarab://launch) - Open Lumafly and launch Hollow Knight.
  - [Launch Vanilla](https://themulhima.github.io/Lumafly/redirect?link=scarab://launch/vanilla) - Open Lumafly and launch un modded Hollow Knight.
  - [Launch Modded](https://themulhima.github.io/Lumafly/redirect?link=scarab://launch/modded) - Open Lumafly and launch modded Hollow Knight.

#### Support Commands

Commands that can be used to help fix problems with Lumafly or Hollow Knight Mods

- [Reset](https://themulhima.github.io/Lumafly/commands/reset) - Opens Lumafly and reset its persistent settings. Equivalent to downloading and opening Lumafly for the first time.
- [Force Update All](https://themulhima.github.io/Lumafly/commands/forceUpdateAll) - Opens Lumafly and reinstalls all mods. Could help fix issues that happened because mods are not downloaded correctly.
- [Remove All Mods Global Settings](https://themulhima.github.io/Lumafly/redirect?link=removeAllModsGlobalSettings) - Opens Lumafly and removes all global settings of mods from saves folder.
- [Use Official Modlinks](https://themulhima.github.io/Lumafly/redirect?link=scarab://useOfficialModLinks) - Opens Lumafly and loads its mod from offcial modlinks.

#### Other Commands

- [Redirect link](https://themulhima.github.io/Lumafly/redirect) You can use this link to redirect to any lumafly command.

### Search by Dependency

Lumafly allows you to search for mods that are dependent or integrated with a specific mod. For example if you want to see Randomizer 4 connections you can search by dependency of randomizer 4 and you will see mods like RandoMapMod, RandoPlus, and BenchRando.

### Settings

There are currently 4 settings that can be changed in Lumafly

- Automatically remove unused dependencies (Default: No) - Do you want lumafly to remove mods that are listed as dependencies of mods that are now being uninstalled.
- Warn before removing a dependent mod (Default: Yes) - Do you want lumafly to warn before disabling or uninstalling a mod that is listed as a required dependency of an installed and enabled mod which could cause it to not function correctly.
- Use custom modlinks (Default: No) - Do you want Lumafly to load its mod list from another source other than the [official modlinks](https://github.com/hk-modding/modlinks). Enabling this will give you the prompt for the custom modlinks URI.
- Game Path - Which install of the game do you want Lumafly to modify.
- Low Storage Mode (Default: False) - Do you want lumafly to not cache mod downloads for faster installs and quicker switching between packs.

### Tools for mod developers

- You can mark a mod as a custom build of the mod by right clicking the mod and clicking on "Register as not in modlinks"
- Pinning a not in modlinks mod ensures Lumafly will never automatically replace it.
- To test if you mod installs correctly, create a fork of [modlinks](https://github.com/hk-modding/modlinks) and in your fork add your mod's manifest in ModLinks.xml. Then open Lumafly and in the settings tab give Lumafly the link to the ModLinks.xml and reload Lumafly.

## Contributions

If you want to suggest a feature or report a bug, report it on the [issues page](https://github.com/TheMulhima/Lumafly/issues/new/choose).  
If you want to contribute to Lumafly, feel free to. You can see what features are currently requested over [here](https://github.com/TheMulhima/Lumafly/labels/enhancement)

### Some open issues you can help with

- [#29](https://github.com/TheMulhima/Lumafly/issues/29) - Translate Lumafly to more languages

## Credits

### Programming

- [56](https://github.com/fifty-six): Creator of Scarab, on which Lumafly is based
- [JacksonFaller](https://github.com/JacksonFaller), [Italy](https://github.com/jngo102), and [Acu1000](https://github.com/Acu1000) - Helped create new features

### Translations

- [Clazex](https://github.com/Clazex) - Chinese translations
- [luiz_eldorado](https://github.com/luizeldorado) - Portuguese translations
- [Dastan](https://github.com/Dastan21) - French translations
- [Adrin](https://twitter.com/Adrin63_?t=lbzYGgt-3Zybjb_S2xqt2A&s=09) and [Helen](https://ko-fi.com/helensb) - Spanish translations
- [Страг](https://discordapp.com/users/274945280775028736) - Russian translations
- [Acu1000](https://github.com/Acu1000) - Polish translations

### Art

- [Dwarfwoot]( https://patreon.com/DwarfWoot), [SFGrenade](https://github.com/SFGrenade) - Images and icons used in the installer.
- [Lime](https://www.tumblr.com/ded-lime) - The Lumafly banner.
- [HBKit](https://ko-fi.com/hbkit) - The Lumafly icon.
