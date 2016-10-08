# Cursed
## The Curse Modpack Downloader

Download entire modpacks from Curse site without needing the Curse launcher. All you need is the project site URL from a modpack
e.g. https://minecraft.curseforge.com/projects/all-the-mods

# This application is written using Eto.Forms, an open source Cross Platform UI library https://github.com/picoe/Eto

## Windows
Extract the contents anywhere, and Double click Cursed.Desktop.exe

## Linux
Requires the Mono runtime. Extract the contents anywhere, and then run Cursed.Desktop.exe using mono e.g. from console "mono ./Cursed.Desktop.exe"
It supports both Gtk2 and Gtk3

## Mac
I can't test this on a Mac, as I don't have one. But it should theoretically work without any code changes. 
You're more than welcome to build for Mac yourself, just make sure to include the Eto mac package in paket.dependencies and paket.references. 
Then follow this guide: https://github.com/picoe/Eto/wiki/Running-your-application