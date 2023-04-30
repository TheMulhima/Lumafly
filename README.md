# Scarab+
![build](https://github.com/TheMulhima/Scarab/actions/workflows/dotnet.yml/badge.svg)
![GitHub all releases](https://img.shields.io/github/downloads/TheMulhima/Scarab/total)

This is a fork of Scarab, a mod manager for Hollow Knight. Why does this exist? Because we felt like there were some 
major features missing from the primary Hollow Knight mod manager that everyone uses.  

This fork has everything that Scarab has plus a few nice features:
- Improved search options (filter by tags, search by dependency)
- A tab for recently released mods
- Important bug fixes for
  - Mods not downloading correctly
  - Showing a mod is updated when it isn't.
  - Showing that Modding API is disabled when it is not.
- And other minor things (queueing multiple downloads, open mod's settings file, button to manually 
install mod, ability to disable/uninstall all)

see below for a [full changelog](#full-changelog-from-scarab)

## What Scarab+ Looks Like
![screenshot](./Readme%20Assets/Default.png)

## Many More Options
![allflyouts](./Readme%20Assets/All%20Flyouts.png)

## Whats New Tab
![whatsnew](./Readme%20Assets/Whats%20new.png)

## More Compact UI
![expanded](./Readme%20Assets/expanded.png)


## Full Changelog from Scarab
#### Additional Features:
- Improved search options
    - Filter by tags
    - Search in mod's descriptions
    - Search for mods that are dependent or integrate with a mod
- Allows queueing multiple downloads at a time (you **don't** have to wait for 1 mod to finish downloading before starting another)
- A new tab that shows all the new mods that were released recently
- Adds a button to open the mod's settings file
- A button to properly place a mod dll/zip to manually install it
- Show all mods that are installed, even if they are manually installed
- Lists authors of mods.
#### Bug Fixes:
- Fixes the bug where scarab sometimes didn't download a mod or update its dependencies properly.
- Update on version difference instead of only on lower version
- Fixed bug where Scarab showed a modded install as vanilla. Instead now if that happens and scarab can't fix the issue by itself, it prompts to verify integrity
- ensures the config file can be written to before starting app
- makes sure that the enabled states of mod actually match if they are in disabled folder or not
#### QoL Changes
- More detailed errors
- Warnings before uninstalling mods that others are dependent on 
- Mod details are more compact.
- Better UI for repository link
- Indication of what mod filter is on
- Button to clear search
- Button to open saves folder
- Allows clickable links in descriptions of mods 
- Adds more bulk actions (Disable All, Uninstall All, Force Update All)

## Usage
- Get the latest release [here](https://github.com/TheMulhima/Scarab/releases/latest).
- Unzip and run it.
- Mods appear in the top left corner of the game title screen after installation.

## Contributions
- Contributions are open. You can see what features are currenly requested over [here](https://github.com/TheMulhima/Scarab/labels/enhancement)
