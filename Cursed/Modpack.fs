namespace Cursed

open System
open System.IO
open System.IO.Compression
open FSharp.Data

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

            using (File.Create(@"C:/tmp/test.zip")) (fun fs -> response.ResponseStream.CopyTo(fs))
            
            ZipFile.ExtractToDirectory(@"C:/tmp/test.zip", @"C:/tmp/test")
        }
        |> Async.RunSynchronously
        |> ignore
        ()