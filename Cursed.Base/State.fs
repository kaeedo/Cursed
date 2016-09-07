namespace Cursed.Base

type AppState =
    { ModpackLink: string 
      ExtractLocation: string }

type StateMessage =
| UpdateLink of string
| DownloadZip
//| None