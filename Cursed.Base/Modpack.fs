namespace Cursed.Base

open System
open System.IO
open FSharp.Data
open PCLStorage
open ICSharpCode.SharpZipLib.Core
open ICSharpCode.SharpZipLib.Zip

type State =
    { Link: string 
      ZipLocation: string }

type Modpack() =
    let mutable cursedState = { Link = ""; ZipLocation = "" }

    let openZip (stream: Stream) =
        let zipInputStream = new ZipInputStream(stream)
        let mutable zipEntry = zipInputStream.GetNextEntry()

        while(zipEntry <> null) do
            if zipEntry.IsDirectory then
                zipEntry <- zipInputStream.GetNextEntry()
            else
                let entryFileName = zipEntry.Name
                let buffer = Array.init 4096 (fun i -> byte(i * i))

                let rootFolder = FileSystem.Current.LocalStorage
                let folder = rootFolder.CreateFolderAsync("Pack", CreationCollisionOption.OpenIfExists) |> Async.AwaitTask |> Async.RunSynchronously
                let file = folder.CreateFileAsync(entryFileName, CreationCollisionOption.ReplaceExisting)  |> Async.AwaitTask |> Async.RunSynchronously
            
                use streamWriter = file.OpenAsync(FileAccess.ReadAndWrite) |> Async.AwaitTask |> Async.RunSynchronously
                StreamUtils.Copy(zipInputStream, streamWriter, buffer)
                zipEntry <- zipInputStream.GetNextEntry()
        ()
    
    member this.UpdateState(state) =
        cursedState <- state

    member this.State
        with get() = cursedState

    member this.DownloadZip(link: string) =
        let modpackLink = if link.EndsWith("/", StringComparison.OrdinalIgnoreCase) then link.Substring(0, link.Length) else link
        let fileUrl = modpackLink + "/files/latest"
        
        async {
            let! response = Http.AsyncRequestStream(fileUrl)

            let readZip = openZip response.ResponseStream
            ()
        }
        |> Async.RunSynchronously
        |> ignore
        ()