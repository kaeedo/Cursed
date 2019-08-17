# Use MultiMC directly
[How to download curse modpacks through multimc](https://www.reddit.com/r/feedthebeast/comments/6yhd90/how_to_download_curse_modpacks_with_only_multimc/)
# No Longer Maintained

# Cursed
## The Curse Modpack Downloader

Download entire modpacks from Curse site without needing the Curse launcher. All you need is the project site URL from a modpack. Make sure it is not the curse.com site (mods.curse.com/modpacks), but the curseforge.com site
e.g. 
https://minecraft.curseforge.com/projects/all-the-mods

Additionally, when using one of the FTB packs, the URL will look something like this
e.g.
https://www.feed-the-beast.com/projects/ftb-presents-skyfactory-2-5

This application is written using Eto.Forms, an open source Cross Platform UI library https://github.com/picoe/Eto

## Usage
If using MultiMC, use the "instances" directory as the download location. It will automatically copy and existing mods where the versions match instead of redownloading them.
The URL must be the curseforge project page. eg:
* https://minecraft.curseforge.com/projects/all-the-mods
* https://www.feed-the-beast.com/projects/ftb-beyond

## Windows
Extract the contents anywhere, and Double click Cursed.Desktop.exe

## Linux
Requires Mono. Run `mono Cursed.Desktop.exe` Tested on Xubuntu after installing the following packages:
* mono-devel (monodevelop on Arch)
* gtk-sharp3 (gtk-sharp-3 on Arch)

## Mac
I can't test this on a Mac, as I don't have one. But it should theoretically work without any code changes. 
You're more than welcome to build for Mac yourself, just make sure to include the Eto mac package in paket.dependencies and paket.references. 
Then follow this guide: https://github.com/picoe/Eto/wiki/Running-your-application
