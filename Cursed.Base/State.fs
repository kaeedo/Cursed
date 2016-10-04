namespace Cursed.Base
open FSharp.Data

type ModpackManifest = JsonProvider<"./SampleManifest.json">

type StateMessage =
| UpdateModpackLink of string
| SetExtractLocation of string
| DownloadZip of AsyncReplyChannel<string>
| UpdateProgress
| None

type ProgressBarState =
| Indeterminate
| Progress of int
| Disabled

type AppState =
    { ModpackLink: string 
      ExtractLocation: string
      Mods: (string * string) list
      ProgressBarState: ProgressBarState }