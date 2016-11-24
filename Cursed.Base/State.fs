namespace Cursed.Base
open FSharp.Data

type ModpackManifest = JsonProvider<"./SampleManifest.json">

type StateMessage =
| UpdateModpackLink of string
| SetExtractLocation of string
| DownloadZip of AsyncReplyChannel<string option>
| UpdateProgress of int
| AddMod of string * int
| FinishDownload

type ProgressBarState =
| Indeterminate
| Progress of int
| Disabled

type Mod =
    { Link: string
      Name: string
      Completed: bool
      ProjectId: int }

type AppState =
    { ModpackLink: string 
      ExtractLocation: string
      Mods: Mod list
      ModCount: int
      ProgressBarState: ProgressBarState }

type StateReplyMessage =
| UpdateModpackLink of string * AsyncReplyChannel<string>
| SetExtractLocation of string * AsyncReplyChannel<string>
| UpdateProgress of int * AsyncReplyChannel<ProgressBarState * Mod list>
| AddMod of string * int * AsyncReplyChannel<Mod list>
| FinishDownload of AsyncReplyChannel<ProgressBarState>