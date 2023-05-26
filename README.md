# LightModding (better name pending)

A Plugin for ChroMapper to assist in modding and in general giving feedback to a mapper. Still very barebones

## How to Install:
Download the `LightModding(version).zip` zip. Drop the `Plugins` folder inside the zip into Chromappers `Plugins` folder.

## How to use

First step is to create a review file for the map, which prepares you for adding notes. You can do so by pressing `tab` or the shortcut to open up the window on the right and selecting the icon with the text `LightModding` to open the main UI.

Currently you can select notes and press `ctrl + alt` (not changeable, havent found a good keybind yet either) to create a comment on them. If there already exists a comment with these specific note
 positions selected then it will go to edit mode. Either way you can now specify the comment and the type/severity. Click `create` when you are done.

Afterwards this opens up the view comment dialog, here you can type a response (useful for the person receiving the mod, dont fill this in as a modder) and mark it as read which will grey out the outline (outlines not implemented yet). From this screen you can also edit the comment information again and you can also delete it there. Click `close` when you are done or `update reply` if you typed in a reply.

When you have put down all the comments you wanted, you can head back to the main UI where you also created the file and you can save it, remove it or copy the comment data to your clipboard. This last option will give you a formatted review that you can paste straight into discord.

To edit a comment, you can select the exact same notes again and press the comment `ctrl + alt` keybind again. Currently this is very hard because there are no outlines yet, but will be much easier when that is done.

If you want to access the review files yourself, they are saved and searched for in a map called `revies` inside the map folder. The file with the latest `FinalizationDate` will always be loaded. Backups are saved ending with `AUTOMATIC_BACKUP.lreview` and will never be automatically loaded. To restore a backup, simply remove `AUTOMATIC_BACKUP` from the file name and make sure you do not have any newer files in the folder.
The plugin will only read files with the .lreview extension and checks for a file structure version. The format is just json.

## Known issues:
* Any text inputs that are 2 menus deep will not properly disable keybinds, making it nearly impossible to type. The workaround is to copy paste your text in instead. There seems to be a bug that is probably caused by me doing something wrong when i create a new dialog box when one is active.

## Currently done:
* main file creation features
* main commenting features
* main review features - done except you cant access them after closing out yet (soonTM)
* saving to json
* autosaving a backup when unloading the editor
* automatically loading review files when found
* editing & deleting comments
* Exporting a review file to clipboard in (discord compatible) markdown

## Currently working on:
* note highlighting
* fixing the weird text input bug
* keybind for opening the reviews to make reviewing them accessible after creation/editing
* editing the file information like title and author

## Planned features:
* Autosaving in intervals
* Save warning when quitting without saving instead of always creating a backup file
* prompting the user about the available review file instead of automatically loading it
* loading review files from a file selector dialog instead of automatically from the map folder
* walking through all the comments step by step
* a menu that lists all comments

## Wishful thinking:
* markers in the scrollbar (like bookmarks)
* Better UI
