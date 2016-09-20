﻿namespace Cursed.Base

open System
open System.Collections.ObjectModel
open System.ComponentModel
open Microsoft.FSharp.Quotations
open Microsoft.FSharp.Quotations.Patterns
open System.IO
open System.IO.Compression
open System.Threading
open FSharp.Data
open Eto.Forms

type ModpackBase() =
    let propertyChanged = new Event<_, _>()
    let toPropName (query: Expr) =
        match query with
        | PropertyGet(a, b, list) -> b.Name
        | _ -> ""

    interface INotifyPropertyChanged with
        [<CLIEvent>]
        member x.PropertyChanged = propertyChanged.Publish

    abstract member OnPropertyChanged: string -> unit
    default this.OnPropertyChanged (propertyName: string) =
        propertyChanged.Trigger(this, new PropertyChangedEventArgs(propertyName))

    member this.OnPropertyChanged (expr: Expr) =
        let propName = toPropName(expr)
        this.OnPropertyChanged(propName)

type Modpack(app: Application) as modpack =
    inherit ModpackBase()
    let mutable extractLocation = ""

    let discoverModpack location =
        location

    let updateLoop =
        let inboxHandler (inbox: MailboxProcessor<StateMessage>) =
            let rec messageLoop oldState = 
                async {
                    let! message = inbox.Receive()

                    match message with
                    | UpdateModpackLink link ->
                        let newState = { oldState with ModpackLink = link }
                        return! messageLoop newState
                    | SetExtractLocation location ->
                        let newState = { oldState with ExtractLocation = location}
                        app.Invoke (fun () -> modpack.ExtractLocation <- newState.ExtractLocation)

                        return! messageLoop newState
                    | DownloadZip ->
                        //CATCH AND HANDLE EXCEPTIONS
                        do modpack.DownloadZip oldState.ModpackLink oldState.ExtractLocation |> Async.RunSynchronously
                        
                        let modlistHtml = Path.Combine([|oldState.ExtractLocation; "modlist.html"|])

                        let! html = HtmlDocument.AsyncLoad(modlistHtml)

                        return! messageLoop oldState
                    | None -> ()
                }

            messageLoop { ModpackLink = ""; ExtractLocation = "" }

        let agent = MailboxProcessor.Start(inboxHandler)
        agent.Error.Add(fun ex ->
            match ex with
            | e -> failwith e.Message
        )
        agent

    member modpack.StateAgent = updateLoop

    member modpack.ExtractLocation
        with get() = extractLocation
        and private set(value) =
            extractLocation <- value
            modpack.OnPropertyChanged <@ modpack.ExtractLocation @>

    member modpack.DownloadZip link location =
        async {
            let modpackLink = if link.EndsWith("/", StringComparison.OrdinalIgnoreCase) then link.Substring(0, link.Length) else link
            let fileUrl = modpackLink + "/files/latest"
        
            let homePath =
                match Environment.OSVersion.Platform with
                | PlatformID.Unix -> Environment.GetEnvironmentVariable("HOME")
                | PlatformID.MacOSX -> Environment.GetEnvironmentVariable("HOME")
                | _ -> Environment.GetFolderPath(Environment.SpecialFolder.Personal)

            let! response = Http.AsyncRequestStream(fileUrl)

            //get fileName from download

            let zipLocation = Path.Combine([|homePath; ".cursedTemp"; "test.zip"|])

            let fileInfo = new FileInfo(zipLocation)
            fileInfo.Directory.Create()

            using(File.Create(zipLocation)) (fun fs -> response.ResponseStream.CopyTo(fs))

            //add subdirectory based on zipname
            ZipFile.ExtractToDirectory(zipLocation, location)
            fileInfo.Delete()
        }
        