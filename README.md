<p align="center"><img src="https://github.com/LightAi39/ChroMapper-LightModding/blob/main/Assets/AutoModderGraphic.png"></p>

---

A Plugin for ChroMapper that assists mappers and modders by implementing auto-checking features as well as comments and replies, with a seamless in-editor experience.

This plugin is primarily aimed at improving the [BeatLeader](https://www.beatleader.xyz/) ranking process by providing mappers with easy-to-use tooling to quickly screen their map for problems with a seamless in-editor experience. In addition, this plugin can ease communication after modding through the comments and reactions features. Auto-checking features are also useful for other applications such as Curation, as many of the values are configurable.

# Installation
Download the ZIP from the latest [release](https://github.com/LightAi39/ChroMapper-AutoModder/releases/latest), then put the contents of the zip into your Chromapper plugins folder.

> :warning: You should use the latest CM version on the regular branch.

# Usage
> For any information you might need, check the [wiki](https://github.com/LightAi39/ChroMapper-AutoModder/wiki)!

The features of this plugin are behind two main buttons, which are both marked with the [BeatLeader icon](https://github.com/LightAi39/ChroMapper-AutoModder/blob/main/ChroMapper-LightModding/Assets/Icon.png).
One is in the song select menu next to the contributor and revert button. This button will let you create and open a review file, and view details and functions.
The other is located in the map editor in the plugins (`tab`) menu. This one will let you access some editor settings.

In addition to these buttons, there are some keybinds which are used for creating and responding to comments.

More information about these keybinds and buttons can be found on the [wiki page.](https://github.com/LightAi39/ChroMapper-AutoModder/wiki/Keybinds-&-Buttons)
A general usage flow can be found below and on the [wiki](https://github.com/LightAi39/ChroMapper-AutoModder/wiki).

## How to use as a mapper
> This is a quickstart guide. Any other information you might want to know is likely documented [in the wiki](https://github.com/LightAi39/ChroMapper-AutoModder/wiki)

First, get familiar with the [keybinds](https://github.com/LightAi39/ChroMapper-AutoModder/wiki/Keybinds-&-Buttons)

Then load a review file by clicking the AutoModder button on the Song Info screen. There you can choose to create a new file. (or automatically load your previously saved file)  

You will now see a menu with some buttons and criteria indicators. Here you can auto-check your song info and each (standard) difficulty. After pressing these buttons the criteria indicators will change colour. Red indicators alert you of issues with that criteria point, and any of these being yellow alerts you to a possible issue.

Inside a difficulty, you will see notes with comments marked in the timeline and outlined.  
Comments will be automatically opened in the top right corner when the grid cursor is on the same beat as a note inside the comment. This overview has buttons for opening and editing a comment.

In addition, you can select a singular note and press the open comment [keybind](https://github.com/LightAi39/ChroMapper-AutoModder/wiki/Keybinds-&-Buttons#keybinds). Currently, this is `Alt+E`. You will then directly go to the view comment menu. For detailed information, refer to the [wiki page](https://github.com/LightAi39/ChroMapper-AutoModder/wiki/Comments).

To create a comment, first select one or more notes. Then, press [one of the keybinds](https://github.com/LightAi39/ChroMapper-AutoModder/wiki/Keybinds-&-Buttons#keybinds) for comment creation. Currently, these are `Ctrl+E` and `Ctrl+Space`. More information is on the [wiki page](https://github.com/LightAi39/ChroMapper-AutoModder/wiki/Comments).

On the pause menu, you will see criteria indicators like before. You can also rerun the auto-checker from here.

Always make sure to [save your file](https://github.com/LightAi39/ChroMapper-AutoModder/wiki/Files#saving-a-file) either in the pause menu or in the song info menu.

## How to use as a modder
> This is a quickstart guide. Any other information you might want to know is likely documented [in the wiki](https://github.com/LightAi39/ChroMapper-AutoModder/wiki)

First, get familiar with the [keybinds](https://github.com/LightAi39/ChroMapper-AutoModder/wiki/Keybinds-&-Buttons)

Then load a review file by clicking the AutoModder button on the Song Info screen. There you can choose to open a file. This will open the file picker. With this, you can select the `.lreview` file you were sent. (or automatically load a previously saved file)  

You will now see a menu with some buttons and criteria indicators. Any of these indicators being red alerts you to an issue with that criteria point, and any of these being yellow alerts you to a possible issue.

Inside a difficulty, you will see notes with comments marked in the timeline and outlined.  
Comments will be automatically opened in the top right corner when the grid cursor is on the same beat as a note inside the comment. This overview has buttons for opening and editing a comment.

In addition, you can select a singular note and press the open comment [keybind](https://github.com/LightAi39/ChroMapper-AutoModder/wiki/Keybinds-&-Buttons#keybinds). Currently, this is `Alt+E`. You will then directly go to the view comment menu. For detailed information, refer to the [wiki page](https://github.com/LightAi39/ChroMapper-AutoModder/wiki/Comments).

To create a comment, first select one or more notes. Then, press [one of the keybinds](https://github.com/LightAi39/ChroMapper-AutoModder/wiki/Keybinds-&-Buttons#keybinds) for comment creation. Currently, these are `Ctrl+E` and `Ctrl+Space`. More information is on the [wiki page](https://github.com/LightAi39/ChroMapper-AutoModder/wiki/Comments).

On the pause menu, you will see criteria indicators like before.

Always make sure to [save your file](https://github.com/LightAi39/ChroMapper-AutoModder/wiki/Files#saving-a-file) either in the pause menu or in the song info menu.

# Contributors and Referenced/used projects

## Contributors
* [Light Ai](https://github.com/LightAi39) - CM plugin side (UI/UX, original comments plugin, basically anything you interact with)
* [Loloppe](https://github.com/Loloppe) - Backend logic/auto-check (BeatmapScanner, Implementing Auto-checking features)

## Referenced/used projects
* [JoshaParity](https://github.com/Joshabi/JoshaParity) by [Joshabi](https://github.com/Joshabi) - Directly integrated fancy parity checker. We love joashabot ♡
* [MapCheck](https://github.com/KivalEvan/BeatSaber-MapCheck) by [KivalEvan](https://github.com/KivalEvan) - Code heavily referenced when writing logic for auto-check.
* [ppCurve](https://github.com/LackWiz/ppCurve/) by [LackWiz](https://github.com/LackWiz) - Code used in the original BeatmapScanner plugin which was implemented in this plugin.
* [ProfanityDetector](https://github.com/stephenhaunts/ProfanityDetector) by [stephenhaunts](https://github.com/stephenhaunts) - Profanity checker used.
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
