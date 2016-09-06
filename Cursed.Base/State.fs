namespace Cursed.Base

type StateUpdate =
| UrlInput of string
| ExtractLocation of string
| None

type AppState =
    { UrlInput: string 
      ExtractLocation: string }

type StateMessage =
| NewState of AppState
| DownloadZip