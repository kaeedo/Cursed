namespace Cursed.Base

open System
open System.IO
open System.IO.Compression
open FSharp.Data
open PCLStorage

type ModpackDiscovery() =
    let readBytes (stream: Stream) =
        let memoryStream = new MemoryStream()
        stream.CopyTo(memoryStream)
        memoryStream.ToArray()

    member this.ExtractFile (entry: ZipArchiveEntry) =
        let rootFolder = FileSystem.Current.LocalStorage
        let fullName = entry.FullName

        if fullName.EndsWith("/") then
            let numberOfDirs = 
                fullName.ToCharArray()
                |> Seq.filter (fun c -> c = '/')
                |> Seq.length

            let rec createDirs dirs (baseDir: IFolder) =
                match dirs with
                | [] -> ()
                | head :: tail -> 
                    if head = "" then
                        ()
                    else
                        let dir = baseDir.CreateFolderAsync(head, CreationCollisionOption.OpenIfExists) |> Async.AwaitTask |> Async.RunSynchronously
                        createDirs tail dir

            match numberOfDirs with
            | 1 -> createDirs [fullName] rootFolder
            | _ -> createDirs (fullName.Split('/') |> List.ofArray) rootFolder
        else
            let filePath = fullName.Split('/') |> List.ofArray

            let rec getBottomDir dirs (baseDir: IFolder) =
                match dirs with
                | [] -> baseDir
                | head :: tail -> 
                    let folder = baseDir.GetFolderAsync(head) |> Async.AwaitTask |> Async.RunSynchronously
                    getBottomDir tail folder

            let createFile (dir: IFolder) (entry: ZipArchiveEntry) =
                let file = dir.CreateFileAsync(entry.Name, CreationCollisionOption.ReplaceExisting) |> Async.AwaitTask |> Async.RunSynchronously
                use stream = file.OpenAsync(FileAccess.ReadAndWrite) |> Async.AwaitTask |> Async.RunSynchronously
                let bytes = readBytes <| entry.Open()
                do stream.WriteAsync(bytes, 0, bytes.Length) |> Async.AwaitTask |> Async.RunSynchronously

            match filePath with
            | [one] -> createFile rootFolder entry
            | ls -> createFile (getBottomDir ls.[..ls.Length - 2] rootFolder) entry