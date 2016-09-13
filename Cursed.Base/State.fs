namespace Cursed.Base

type AppState =
    { ModpackLink: string 
      ExtractLocation: string }

type StateMessage =
| UpdateModpackLink of string
| SetExtractLocation of string
| DownloadZip
| None