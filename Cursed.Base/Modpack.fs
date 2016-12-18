﻿namespace Cursed.Base

open System
open System.ComponentModel
open System.IO
open System.Net
open Microsoft.FSharp.Quotations
open Microsoft.FSharp.Quotations.Patterns

open FSharp.Data
open Hopac
open HttpFs.Client
open Eto.Forms
open Common
open ModpackController

type ModpackBase() =
    let propertyChanged = new Event<_, _>()
    let toPropName (query: Expr) =
        match query with
        | PropertyGet(a, b, list) -> b.Name
        | _ -> String.Empty

    interface INotifyPropertyChanged with
        [<CLIEvent>]
        member x.PropertyChanged = propertyChanged.Publish

    abstract member OnPropertyChanged: string -> unit
    default this.OnPropertyChanged (propertyName: string) =
        propertyChanged.Trigger(this, new PropertyChangedEventArgs(propertyName))

    member this.OnPropertyChanged (expr: Expr) =
        let propName = toPropName(expr)
        this.OnPropertyChanged(propName)

type Modpack(app: Application) as this =
    inherit ModpackBase()
    do ServicePointManager.DefaultConnectionLimit <- 1000

    let mutable modpackLink = String.Empty
    let mutable extractLocation = String.Empty
    let mutable mods = [{ Link = String.Empty; Name = String.Empty; Completed = false; ProjectId = 0 }]
    let mutable modCount = 0
    let mutable progressBarState = Disabled

    member this.UpdateModpackLink link =
        job {
            this.ModpackLink <- ViewActor.UpdateLoop.PostAndReply (fun reply -> UpdateModpackLink (link, reply))
        }
        |> start

    member this.SetExtractLocation link =
        job {
            this.ExtractLocation <- ViewActor.UpdateLoop.PostAndReply (fun reply -> SetExtractLocation (link, reply))
        }
        |> start

    member this.UpdateProgress projectId =
        job {
            let progressBarState, mods = ViewActor.UpdateLoop.PostAndReply (fun reply -> UpdateProgress (projectId, reply))
            this.ProgressBarState <- progressBarState
            this.Mods <- mods
        }
        |> start

    member this.AddMod (modName, projectId) =
        job {
            this.Mods <- ViewActor.UpdateLoop.PostAndReply (fun reply -> AddMod (modName, projectId, reply))
        }
        |> start

    member this.FinishDownload =
        job {
            this.ProgressBarState <- ViewActor.UpdateLoop.PostAndReply FinishDownload
        }
        |> start

    member this.ModpackLink
        with get() = modpackLink
        and private set(value) =
            modpackLink <- value

    member this.ExtractLocation
        with get() = extractLocation
        and private set(value) =
            extractLocation <- value
            app.Invoke (fun () -> this.OnPropertyChanged <@ this.ExtractLocation @>)

    member this.Mods
        with get() = mods
        and private set(value) =
            mods <- value
            app.Invoke (fun () -> this.OnPropertyChanged <@ this.Mods @>)
        
    member this.ModCount
        with get() = modCount
        and private set(value) =
            modCount <- value
            app.Invoke (fun () -> this.OnPropertyChanged <@ this.ModCount @>)

    member this.ProgressBarState
        with get() = progressBarState
        and private set(value) =
            progressBarState <- value
            app.Invoke (fun () -> this.OnPropertyChanged <@ this.ProgressBarState @>)

    member this.DownloadMod location (file: ModpackManifest.File) =
        job {
            let projectResponse =
                Request.create Get (Uri <| sprintf "http://minecraft.curseforge.com/projects/%i" file.ProjectId)
                |> getResponse
                |> run

            let link = projectResponse.responseUri.ToString()
            let html = HtmlDocument.Load(link)

            let modName = (html.CssSelect("h1.project-title > a > span")).[0].InnerText

            this.AddMod (modName (), file.ProjectId)

            let fileUrl = sprintf "%A/files/%i/download" projectResponse.responseUri file.FileId

            using(Request.create Get (Uri fileUrl) |> getResponse |> run) (fun r ->
                let fileName = Uri.UnescapeDataString(r.responseUri.Segments |> Array.last)

                let modsDirectory = location @@ "mods"
                Directory.CreateDirectory(modsDirectory) |> ignore

                using(new FileStream(modsDirectory @@ fileName, FileMode.Create)) (fun s -> 
                    r.body.CopyTo(s)
                    s.Close()
                )
            )

            this.UpdateProgress file.ProjectId
        }

    member this.DownloadZip =
        this.ProgressBarState <- Indeterminate

        let zipInformation = DownloadZip this.ModpackLink this.ExtractLocation
        match zipInformation with
        | Choice1Of2 zipInfo ->
            let subdirectory = ExtractZip this.ExtractLocation zipInfo

            let manifestFile = File.ReadAllLines(subdirectory @@ "manifest.json") |> Seq.reduce (+)
            let manifest = ModpackManifest.Parse(manifestFile)

            job {
                let modCount, progressBarState = ViewActor.UpdateLoop.PostAndReply (fun reply -> UpdateModpackInformation (manifest.Files.Length, reply))
                this.ModCount <- modCount
                this.ProgressBarState <- progressBarState
            }
            |> start

            DirectoryCopy (subdirectory @@ "overrides") subdirectory
            Some subdirectory
        | Choice2Of2 _ ->
            this.ProgressBarState <- Disabled

            None