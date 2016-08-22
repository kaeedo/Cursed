namespace Cursed.Base

open System
open System.IO
open System.IO.Compression
open FSharp.Data
open PCLStorage

type State =
    { Link: string 
      ZipLocation: string }

type Modpack() =
    let mutable cursedState = { Link = ""; ZipLocation = "" }

    let modpackDiscovery = new ModpackDiscovery()

    member this.UpdateState(state) =
        cursedState <- state

    member this.State
        with get() = cursedState

    member this.DownloadZip(link: string) =
        let modpackLink = if link.EndsWith("/", StringComparison.OrdinalIgnoreCase) then link.Substring(0, link.Length) else link
        let fileUrl = modpackLink + "/files/latest"
        
        async {
            let! response = Http.AsyncRequestStream(fileUrl)

            let resposneStreamBytes = 
                let stream = new MemoryStream()
                response.ResponseStream.CopyTo(stream)
                stream.ToArray()

            let rootFolder = FileSystem.Current.LocalStorage
            let zipName = response.ResponseUrl.Substring(response.ResponseUrl.LastIndexOf('/'))
            let! file = rootFolder.CreateFileAsync(zipName, CreationCollisionOption.ReplaceExisting) |> Async.AwaitTask

            use! stream = file.OpenAsync(FileAccess.ReadAndWrite) |> Async.AwaitTask
            do! stream.WriteAsync(resposneStreamBytes, 0, resposneStreamBytes.Length) |> Async.AwaitTask

            use archive = new ZipArchive(stream)
            
            archive.Entries
            |> Seq.iter modpackDiscovery.ExtractFile
        }
        |> Async.RunSynchronously
        |> ignore
        ()