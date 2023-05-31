# LightModding (better name pending)

A Plugin for ChroMapper to assist in modding and in general giving feedback to a mapper.

## How to Install:
Download the `LightModding(version).zip` zip. Drop the `Plugins` folder inside the zip into Chromappers `Plugins` folder.

> :warning: This plugin only supports CM versions after V3 support was added! Currently you need the dev branch version for this.

## How to use as a modder/reviewer

First step is to create a review file for the map, which prepares you for adding notes. You can do so by pressing `tab` or the shortcut to open up the window on the right and selecting the icon with the text `LightModding` to open the main UI.

Currently you can select notes and press `ctrl + g` (not changeable) to create a comment on them. If there already exists a comment with these specific note
 positions selected then it will go to edit mode. Either way you can now specify the comment and the type/severity. Click `create` when you are done.

Afterwards this opens up the view comment dialog, here you can type a response (useful for the person receiving the mod, dont fill this in as a modder) and mark it as read which will grey out the outline.
From this screen you can also edit the comment information again and you can also delete it there. Click `close` when you are done or `update reply` if you typed in a reply.

When you have put down all the comments you wanted, you can head back to the main UI where you also created the file and you can save it, remove it or copy the comment data to your clipboard. This last option will give you a formatted review that you can paste straight into discord.

To edit a comment, you can select the exact same notes again and press the comment `ctrl + g` keybind again. You can tell there is a comment on a note when it has an outline.

You can edit file information in the main ui by pressing `edit file information`, you can add an overall comment to the review here too.

## How to use as a mapper recieving a review file

Until dialog selection is implemented, you have to create a folder called `reviews` in your map folder. You can easily get here by pressing `open explorer` on the song info screen. Place the `.lreview` file inside the folder you created.
Make sure there are no newer review files in the folder as those would be loaded instead.

Now you can open the map. You can check if the file loaded by pressing `tab` or the shortcut to open up the window on the right and selecting the icon with the text `LightModding` to open the main UI.
Check if the menu that pops up says `Existing review file loaded!` and the path is what you expected. Now close out of that menu.

Try selecting a single highlighted note and pressing `alt + g` (not changeable). You should see a menu pop up. If you see a menu asking you to choose a comment, then there are multiple comments which include that note and you have to choose one.
If you see a menu called `View comment` there was only one comment for the note.

You should now be on the `View Comment` screen either way. Here you can type in a response to the comment and you can mark it as read. Press `update reply` if you want to save any changes you made.
Note that some comment data will be hard to access inside the plugin when the map data gets changed, as it can only know the note positions when the comment was created. You can always look at all the comments inside the main UI, click `show all comments` there.

As someone receiving the file, you should ignore the `edit comment` button as this will show you a menu to change the comment itself. Press `close` to exit the menu.

If you want to re-export or save with your responses you can simply open up the side menu with `tab` or your shortcut and press the mod icon. Inside the Main UI there is a button called copy comments to clipboard.
You can simply paste the text into discord after that. You can save the map by pressing `save review file`.

## Advanced use and backup usage

If you want to access the review files yourself, they are saved and searched for in a map called `revies` inside the map folder. The file with the latest `FinalizationDate` will always be loaded. Backups are saved ending with `AUTOMATIC_BACKUP.lreview` and will never be automatically loaded. To restore a backup, simply remove `AUTOMATIC_BACKUP` from the file name and make sure you do not have any newer files in the folder.
The plugin will only read files with the .lreview extension and checks for a file structure version. The format is just json.

## Known issues:
* Any text inputs that are 2 menus deep will not properly disable keybinds, making it nearly impossible to type. The workaround is to copy paste your text in instead. There seems to be a bug that is probably caused by me doing something wrong when i create a new dialog box when one is active.
* drop down menus don't show their initial value when set. This is only visual and does not affect the data.

## Currently done:
* main file creation features
* main commenting features
* main review features
* saving to json
* autosaving a backup when unloading the editor
* automatically loading review files when found
* editing & deleting comments
* Exporting a review file to clipboard in (discord compatible) markdown
* note highlighting
* highlight toggle in main ui
* UI for choosing between multiple comments on a note
* keybind for opening the reviews to make reviewing them accessible after creation/editing
* a menu that lists all comments
* editing the file information like title and author
* adding an overall comment to the review
* exporting a review file to clipboard in a compact way (beatleader comments)

## Currently working on:
* fixing the weird text input bug

## Planned features:
* Autosaving in intervals
* Save warning when quitting without saving instead of always creating a backup file
* loading review files from a file selector dialog instead of automatically from the map folder

## Wishful thinking:
* markers in the scrollbar (like bookmarks)
* Better UI
* walking through all the comments step by step
