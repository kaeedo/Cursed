namespace Cursed.Base

open System
open System.IO
open System.Net

open FSharp.Data
open Hopac
open HttpFs.Client
open Eto.Forms
open Common
open ModpackController

module Handler =
    let myHandler sender (args: UnhandledExceptionEventArgs) =
        args.ExceptionObject :?> Exception

type Modpack(app: Application) as this =
    inherit NotifyPropertyChanged()
    do
        ServicePointManager.DefaultConnectionLimit <- 1000
        CacheActor.FileLoop.Post Load
        async {
            do! Async.Sleep 1000
            this.Load
        } |> Async.Start

    let mutable mods = [{ Link = String.Empty; Name = String.Empty; Completed = false; ProjectId = 0 }]
    let mutable modCount = 0
    let mutable progressBarState = Disabled
    let mutable modpackLink = String.Empty
    let mutable extractLocation = String.Empty

    member this.UpdateModpackLink link =
        this.ModpackLink <- ViewActor.UpdateLoop.PostAndReply (fun reply -> UpdateModpackLink (link, reply))

    member this.SetExtractLocation link =
        this.ExtractLocation <- ViewActor.UpdateLoop.PostAndReply (fun reply -> SetExtractLocation (link, reply))

    member this.UpdateProgress projectId =
        let progressBarState, mods = ViewActor.UpdateLoop.PostAndReply (fun reply -> UpdateProgress (projectId, reply))
        this.ProgressBarState <- progressBarState
        this.Mods <- mods

    member this.AddMod (modName, projectId) =
        this.Mods <- ViewActor.UpdateLoop.PostAndReply (fun reply -> AddMod (modName, projectId, reply))

    member this.FinishDownload =
        this.ProgressBarState <- ViewActor.UpdateLoop.PostAndReply FinishDownload

    member this.ModpackLink
        with get() = modpackLink
        and private set(value) =
            modpackLink <- value
            app.Invoke (fun () -> this.OnPropertyChanged <@ this.ModpackLink @>)

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

    member this.Load =
        let cache = CacheActor.FileLoop.PostAndReply GetCache
        this.ModpackLink <- cache.CurseLink
        this.ExtractLocation <- cache.ModpackLocation

    member this.DownloadMod location (file: ModpackManifest.File) =
        let saveToCache projectId modName fileId fileName =
            CacheActor.FileLoop.Post <| SaveProject { Id = projectId; Name = modName; Files = [] }
            CacheActor.FileLoop.Post <| SaveMod (projectId, { Id = fileId; FileName = fileName })

        let cache = CacheActor.FileLoop.PostAndReply GetCache
        let cachedMod =
            cache.Projects
                |> List.tryFind (fun p ->
                    p.Id = file.ProjectId
                )

        let modsDirectory = location @@ "mods"

        let maybeCopyMod =
            maybe {
                let! cachedMod = cachedMod

                do! Some (this.AddMod (cachedMod.Name, cachedMod.Id))

                let! file =
                    cachedMod.Files
                    |> List.tryFind (fun f ->
                        f.Id = file.FileId
                    )

                let! tryFindModLocation = TryFindMod this.ExtractLocation file.FileName

                return tryFindModLocation
            }

        match maybeCopyMod with
        | Some copyModLocation ->
            job {
                 File.Copy(copyModLocation, modsDirectory @@ Path.GetFileName(copyModLocation), true)
                 this.UpdateProgress file.ProjectId
            }
        | None ->
            job {
                let projectResponse =
                    Request.create Get (Uri <| sprintf "http://minecraft.curseforge.com/projects/%i" file.ProjectId)
                    |> getResponse
                    |> run

                let link = projectResponse.responseUri.ToString()
                let html = HtmlDocument.Load(link)

                let modName =
                    match cachedMod with
                    | Some project -> project.Name
                    | None ->
                        let modNameHtml = (html.CssSelect("h1.project-title > a > span")).[0].InnerText
                        let name = modNameHtml()
                        this.AddMod (name, file.ProjectId)
                        name

                let fileUrl = sprintf "%A/files/%i/download" projectResponse.responseUri file.FileId

                using(Request.create Get (Uri fileUrl) |> getResponse |> run) (fun r ->
                    let fileName = Uri.UnescapeDataString(r.responseUri.Segments |> Array.last)

                    saveToCache file.ProjectId modName file.FileId fileName

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

            let modCount, progressBarState = ViewActor.UpdateLoop.PostAndReply (fun reply -> UpdateModpackInformation (manifest.Files.Length, reply))
            this.ModCount <- modCount
            this.ProgressBarState <- progressBarState

            DirectoryCopy (subdirectory @@ "overrides") subdirectory
            Some subdirectory
        | Choice2Of2 _ ->
            this.ProgressBarState <- Disabled

            None
