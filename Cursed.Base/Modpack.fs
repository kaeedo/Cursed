namespace Cursed.Base

open System
open FSharp.Data
open PCLStorage

type State =
    { Link: string 
      ZipLocation: string }

type Modpack() =
    let mutable cursedState = { Link = ""; ZipLocation = "" }
    
    member this.UpdateState(state) =
        cursedState <- state

    member this.State
        with get() = cursedState

    member this.DownloadZip(link: string) =
        let modpackLink = if link.EndsWith("/", StringComparison.OrdinalIgnoreCase) then link.Substring(0, link.Length) else link
        let fileUrl = modpackLink + "/files/latest"
        
        async {
            let! response = Http.AsyncRequestStream(fileUrl)

            let rootFolder = FileSystem.Current.LocalStorage
            let! folder = rootFolder.CreateFolderAsync("Pack", CreationCollisionOption.OpenIfExists) |> Async.AwaitTask
            let! file = folder.CreateFileAsync("pack.zip", CreationCollisionOption.ReplaceExisting) |> Async.AwaitTask

            use! stream = file.OpenAsync(FileAccess.ReadAndWrite) |> Async.AwaitTask
            do! response.ResponseStream.CopyToAsync(stream) |> Async.AwaitTask
        }
        |> Async.RunSynchronously
        |> ignore
        ()