# Lumafly Website

[![website](https://img.shields.io/website?down_color=red&down_message=offline&up_color=32c854&up_message=online&url=https%3A%2F%2Fthemulhima.github.io%2FScarab)](https://themulhima.github.io/Lumafly)

This branch exists to deploy a github page that serves Lumafly website. It serves the following purposes

## How to build locally

- Install ruby
- Run the command `gem install github-pages`
- Clone the git directory such that its parent folder is also called Scarab.
- In the parent Scarab folder run `jekyll build`
- To run the website, run the command `jekyll serve`.
- Open the website at <http://127.0.0.1:4000/Lumafly/>
Note: it is done like this because in the end it will be <https://themulhima.github.io/Lumafly>. So we need to replicate that

| Purpose | Link | Description |
|---------|------|-----------|
| Landing Page | <https://themulhima.github.io/Lumafly> | Main page for Lumafly website. Hold link for download and links for other important things (commands page, readme, discord, etc.).
| Direct Download Link | <https://themulhima.github.io/Lumafly?download> | A direct download link that can be used to automatically download the latest release.
| Direct Update Link | <https://themulhima.github.io/Lumafly?download=update> | Similar to direct download link but shows the latest version's release notes.
| Latest Download Link | <https://themulhima.github.io/Lumafly?download=latest> | A direct download link that can be used to automatically download the latest build of the master branch.
| Commands Page | <https://themulhima.github.io/Lumafly/commands> | Allows the creation of shareable links that can open and run commands in Lumafly.
| Download Command Page | <https://themulhima.github.io/Lumafly/commands/download> | Allows the creation shareable download links for mods using Lumafly.
| Custom Modlink Command Page | <https://themulhima.github.io/Lumafly/commands/customModLinks> | Allows the creation of shareable links that will open Lumafly and load the mod list from a custom Modlinks.
| Force Update All Command Page | <https://themulhima.github.io/Lumafly/commands/forceUpdateAll> | Allows the creation of shareable links that will open Lumafly and cause all installed mods to forcefully reinstall and update.
| Reset Command Page | <https://themulhima.github.io/Lumafly/commands/reset> | Allows the creation of shareable links that will open Lumafly and reset its persistent settings.
| Redirect Page | <https://themulhima.github.io/Lumafly/commands/redirect> | Allows the creation of shareable links of any Scarab Command.
