<p align="center"><img src="https://github.com/LightAi39/ChroMapper-LightModding/blob/main/Assets/AutoModderGraphic.png"></p>

---

A Plugin for ChroMapper that assists mappers and modders by implementing auto-checking features as well as comments and replies, with a seamless in-editor experience.

This plugin is primarily aimed at improving the [BeatLeader](https://www.beatleader.xyz/) ranking process by providing mappers with easy-to-use tooling to quickly screen their map for problems with a seamless in-editor experience. In addition, this plugin can ease communication after modding through the comments and reactions features. Auto-checking features are also useful for other applications such as Curation, as many of the values are configurable.

# Installation
Download the ZIP from the latest [release](https://github.com/LightAi39/ChroMapper-AutoModder/releases/latest), then put the contents of the zip into your Chromapper plugins folder.

> :warning: This plugin only supports CM versions after V3 support was added! Currently, you need to be on the dev branch for this.

# Usage
> For any information you might need, check the [wiki](https://github.com/LightAi39/ChroMapper-AutoModder/wiki)!

The features of this plugin are behind two main buttons, which are both marked with the [BeatLeader icon](https://github.com/LightAi39/ChroMapper-AutoModder/blob/main/ChroMapper-LightModding/Assets/Icon.png).
One is in the song select menu next to the contributor and revert button. This button will let you create and open a review file, and view details and functions.
The other is located in the map editor in the plugins (`tab`) menu. This one will let you access some editor settings.

In addition to these buttons, there are some keybinds which are used for creating and responding to comments.

More information about these keybinds and buttons can be found on the [wiki page.](https://github.com/LightAi39/ChroMapper-AutoModder/wiki/Keybinds-&-Buttons)
A general usage flow can be found below and on the [wiki](https://github.com/LightAi39/ChroMapper-AutoModder/wiki).

## How to use as a mapper
coming soon


## How to use as a modder
coming soon


# Contributors and Referenced/used projects

## Contributors
* [Light Ai](https://github.com/LightAi39) - CM plugin side (UI/UX, original comments plugin, basically anything you interact with)
* [Loloppe](https://github.com/Loloppe) - Backend logic/auto-check (BeatmapScanner, Implementing Auto-checking features)

## Referenced/used projects
* [JoshaParity](https://github.com/Joshabi/JoshaParity) by [Joshabi](https://github.com/Joshabi) - Directly integrated fancy parity checker. We love joashabot ♡
* [MapCheck](https://github.com/KivalEvan/BeatSaber-MapCheck) by [KivalEvan](https://github.com/KivalEvan) - Code heavily referenced when writing logic for auto-check.
* [Newtonsoft.Json](https://github.com/jamesnk/newtonsoft.json) by [JamesNK](https://github.com/JamesNK) - Used for object (de)serialization for saving/loading.

# Features

## Current Features:
* Creating comments and responding to those comments
* Highlighting notes with comments
* Timeline markers for comments
* Seeing a list of all comments
* Comment preview when on the same beat as a comment
* Auto-checking map for issues like vision blocks, parity errors, and other [BeatLeader Criteria](https://beatleader.wiki/en/criteria) points.
* Status indicators for Criteria points
* Exporting comments to text

## Known issues:
* Some text inputs might not disable keybinds. If this is the case, copy-paste text in. (CMUI)
* Drop-down menus don't show their current value when loaded. This is only visual and does not affect the data. (CMUI)

## Coming next:
* Improvements and additions to the auto-checker.

# Suggestions, questions or issues
Please feel free to create a post in the [BeatLeader Discord](https://discord.gg/2RG5YVqtG6) under the AutoModder forum if you have any suggestions, questions, or if you encountered an issue. You are also welcome to open an Issue or PR, but using Discord will likely get a faster response.
