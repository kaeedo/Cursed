namespace Cursed.Base

type AppState =
    { ModpackLink: string 
      ExtractLocation: string
      Mods: (string * string) list }

type StateMessage =
| UpdateModpackLink of string
| SetExtractLocation of string
| DownloadZip
| None