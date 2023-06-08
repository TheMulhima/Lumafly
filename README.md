# Scarab+ Website

[![website](https://img.shields.io/website?down_color=red&down_message=offline&up_color=32c854&up_message=online&url=https%3A%2F%2Fthemulhima.github.io%2FScarab)](https://themulhima.github.io/Scarab)

This branch exists to deploy a github page that serves Scarab+ website. It serves the following purposes

## How to build locally

- Install ruby
- Run the command `gem install github-pages`
- Clone the git directory such that its parent folder is also called Scarab.
- In the parent Scarab folder run `jekyll build`
- To run the website, run the command `jekyll serve`.
- Open the website at <http://127.0.0.1:4000/Scarab/>
Note: it is done like this because in the end it will be <https://themulhima.github.io/Scarab>. So we need to replicate that

| Purpose | Link | Description |
|---------|------|-----------|
| Landing Page | <https://themulhima.github.io/Scarab> | Main page for Scarab+ website. Hold link for download and links for other important things (commands page, readme, discord, etc.).
| Direct Download Link | <https://themulhima.github.io/Scarab?download> | A direct download link that can be used to automatically download the latest release.
| Direct Update Link | <https://themulhima.github.io/Scarab?download=update> | Similar to direct download link but shows the latest version's release notes.
| Latest Download Link | <https://themulhima.github.io/Scarab?download=latest> | A direct download link that can be used to automatically download the latest build of the master branch.
| Commands Page | <https://themulhima.github.io/Scarab/commands> | Allows the creation of shareable links that can open and run commands in Scarab+.
| Download Command Page | <https://themulhima.github.io/Scarab/commands/download> | Allows the creation shareable download links for mods using Scarab+.
| Custom Modlink Command Page | <https://themulhima.github.io/Scarab/commands/customModLinks> | Allows the creation of shareable links that will open Scarab+ and load the mod list from a custom Modlinks.
| Force Update All Command Page | <https://themulhima.github.io/Scarab/commands/forceUpdateAll> | Allows the creation of shareable links that will open Scarab+ and cause all installed mods to forcefully reinstall and update.
| Reset Command Page | <https://themulhima.github.io/Scarab/commands/reset> | Allows the creation of shareable links that will open Scarab+ and reset its persistent settings.
| Redirect Page | <https://themulhima.github.io/Scarab/commands/redirect> | Allows the creation of shareable links of any Scarab Command.
