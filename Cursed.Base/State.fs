namespace Cursed.Base
open FSharp.Data

type ModpackManifest = JsonProvider<"./SampleManifest.json">

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

type ModFile =
    { Id: int
      FileName: string }

type Project =
    { Id: int
      Name: string
      Files: ModFile list }

type StateReplyMessage =
| UpdateModpackLink of string * AsyncReplyChannel<string>
| SetExtractLocation of string * AsyncReplyChannel<string>
| UpdateProgress of int * AsyncReplyChannel<ProgressBarState * Mod list>
| AddMod of string * int * AsyncReplyChannel<Mod list>
| UpdateModpackInformation of int * AsyncReplyChannel<int * ProgressBarState>
| FinishDownload of AsyncReplyChannel<ProgressBarState>
| Restart

type FileReplyMessage =
| SaveProject of Project
| SaveMod of projectId: int * ModFile
| GetCache of AsyncReplyChannel<Project list>
| Load of Project list
| Restart