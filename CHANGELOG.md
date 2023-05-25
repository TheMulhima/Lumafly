## Changes in v2.1.0.0
- Add French Translations (by @Dastan21).
- Add URL scheme handling for linux.
- Fix not installed mods showing in ask prompt for uninstalled unused dependencies.
- Catch any error while getting latest version (instead of only HTTP and Timeout). This prevents random crashes on load.

## Changes in v2.0.0.0
#### Additional Features:
- Improved search options (Filter by tags, by authors, search for dependents).
- An offline mode.
- Filter for newly released/updated mods.
- Ability to pin mods. (by @JacksonFaller)
- Manually install button.
- Auto uninstall unused dependencies. (by @JacksonFaller)
- Show all mods that are installed, even if they are manually installed.
- Adds a button to open any mod's global settings file.
- Ability to load Scarab's mod list from custom modlinks.
- Lists authors of mods.
- A settings tab for scarab's settings.
#### Bug Fixes:
- Fixes the bug where scarab sometimes didn't download a mod or update its dependencies properly.
- Update on version difference instead of only on lower version.
- Fixed bug where Scarab showed a modded install as vanilla. Instead now if that happens and scarab can't fix the issue by itself, it prompts to verify integrity.
- Ensures the config file can be written to before starting app.
- Makes sure that the enabled states of mod actually match if they are in disabled folder or not.
- Better handling for MS Store version of the game.
- Checks dependencies are installed when toggling on a mod. (by @JacksonFaller)
- Fixed Scarab showing blank screen on initial load.
#### QoL Changes
- More detailed errors.
- Mod details are more compact.
- Better UI for repository link.
- Indication of what mod filter is on.
- Button to clear search.
- Button to open saves folder.
- Allows clickable links in descriptions of mods.
- Adds more bulk actions (Disable All, Uninstall All, Update All, Enable All).

### Credits
- [TheMulhima](https://github.com/TheMulhima) - Most of the changes
- [Clazex](https://github.com/Clazex) - Chinese translations and compatibility
- [JacksonFaller](https://github.com/JacksonFaller) - Pinning Mods, Auto uninstall unused dependencies, check dependencies are installed when toggling on a mod.
- [Italy](https://github.com/jngo102) - Tabs
- [luiz_eldorado](https://github.com/luizeldorado) - Portuguese translations
- [Dastan](https://github.com/Dastan21) - French translations