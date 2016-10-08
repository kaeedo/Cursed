namespace Cursed.Base
open FSharp.Data

type ModpackManifest = JsonProvider<"./SampleManifest.json">

type Releases = JsonProvider<"./SampleReleases.json">

type StateMessage =
| UpdateModpackLink of string
| SetExtractLocation of string
| DownloadZip of AsyncReplyChannel<string option>
| UpdateProgress of int

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
      ProgressBarState: ProgressBarState }