namespace Cursed.Base

open System
open System.ComponentModel
open System.IO
open System.IO.Compression
open System.Net
open Microsoft.FSharp.Quotations
open Microsoft.FSharp.Quotations.Patterns

open FSharp.Data
open Hopac
open HttpFs.Client
open Eto.Forms
open Operators

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

    let rec directoryCopy sourcePath destinationPath =
        Directory.CreateDirectory(destinationPath) |> ignore

        let sourceDirectory = new DirectoryInfo(sourcePath)
        sourceDirectory.GetFiles()
        |> Seq.iter (fun f ->
            f.CopyTo(destinationPath @@ f.Name, true) |> ignore
        )

        sourceDirectory.GetDirectories()
        |> Seq.iter (fun d ->
            directoryCopy d.FullName (destinationPath @@ d.Name) |> ignore
        )

    let downloadZip (link: string) location =
        job {
            let modpackLink = if link.EndsWith("/", StringComparison.OrdinalIgnoreCase) then link.Substring(0, link.Length) else link
            let fileUrl = modpackLink + "/files/latest"
        
            let homePath =
                match Environment.OSVersion.Platform with
                | PlatformID.Unix -> Environment.GetEnvironmentVariable("HOME")
                | PlatformID.MacOSX -> Environment.GetEnvironmentVariable("HOME")
                | _ -> Environment.GetFolderPath(Environment.SpecialFolder.Personal)

            use! response =
                Request.create Get (Uri fileUrl)
                |> getResponse

            let zipName = Uri.UnescapeDataString(response.responseUri.Segments |> Array.last)
            let zipLocation = homePath @@ ".cursedTemp"

            use fileStream = new FileStream(zipLocation @@ zipName, FileMode.Create)
            do! response.body.CopyToAsync fileStream |> Job.awaitUnitTask

            return zipName, zipLocation
        }
        |> Job.catch
        |> run

    let extractZip location ((zipName: string), (zipLocation: string)) =
        let modpackSubdirectory = zipName.Substring(0, zipName.LastIndexOf('.'))
        let extractLocation = location @@ modpackSubdirectory
        ZipFile.ExtractToDirectory(zipLocation @@ zipName, extractLocation)

        let fileInfo = new FileInfo(zipLocation @@ zipName)
        fileInfo.Delete()

        modpackSubdirectory
    
    let updateLoop =
        let inboxHandler (inbox: MailboxProcessor<StateMessage>) =
            let rec messageLoop oldState = 
                async {
                    let! message = inbox.Receive()
                    
                    match message with
                    | UpdateModpackLink link ->
                        let newState = { oldState with ModpackLink = link }
                        this.ModpackLink <- link

                        return! messageLoop newState
                    | SetExtractLocation location ->
                        let newState = { oldState with ExtractLocation = location}
                        this.ExtractLocation <- newState.ExtractLocation

                        return! messageLoop newState
                    | DownloadZip reply ->
                        this.ProgressBarState <- Indeterminate

                        let zipInformation = downloadZip oldState.ModpackLink oldState.ExtractLocation
                        match zipInformation with
                        | Choice2Of2 _ ->
                            this.ProgressBarState <- Disabled
                            reply.Reply None
                            return! messageLoop oldState
                        | Choice1Of2 zipInfo ->
                            let subdirectory = extractZip oldState.ExtractLocation zipInfo
                            let modlistHtml = oldState.ExtractLocation @@ subdirectory @@ "modlist.html"
                            let html = HtmlDocument.Load(modlistHtml)
                        
                            let links = 
                                html.Descendants ["a"]
                                |> Seq.choose (fun a ->
                                    a.TryGetAttribute("href")
                                    |> Option.map (fun attr ->
                                        let modText = a.InnerText().[0..0].ToUpper() + a.InnerText().[1..]
                                        modText, attr.Value()
                                    ) 
                                )
                                |> Seq.sort
                                |> Seq.map (fun l ->
                                    let name, link = l
                                    let projectId = link.Split('/') |> Seq.last
                                    { Link = link; Name = name; Completed = false; ProjectId = Int32.Parse(projectId) }
                                )
                                |> List.ofSeq
                        
                            let newState = { oldState with Mods = links; ProgressBarState = Indeterminate }
                            this.Mods <- links
                        
                            directoryCopy (oldState.ExtractLocation @@ subdirectory @@ "overrides") (oldState.ExtractLocation @@ subdirectory)
                            reply.Reply (Some (oldState.ExtractLocation @@ subdirectory))

                            return! messageLoop newState
                    | UpdateProgress projectId ->
                        let progress =
                            let previousProgress =
                                match oldState.ProgressBarState with
                                | Progress numberCompleted -> numberCompleted
                                | _ -> 0
                            ProgressBarState.Progress (previousProgress + 1)

                        let finishedMod = 
                            oldState.Mods
                            |> List.find (fun m ->
                                m.ProjectId = projectId
                            )

                        let updateMods = 
                            oldState.Mods
                            |> List.map (fun m ->
                                if m = finishedMod then
                                    { finishedMod with Completed = true }
                                else
                                    m
                            )

                        let newState = { oldState with ProgressBarState = progress; Mods = updateMods }
                        this.ProgressBarState <- progress
                        this.Mods <- updateMods

                        return! messageLoop newState
                }

            messageLoop { ModpackLink = String.Empty
                          ExtractLocation = String.Empty
                          Mods = []
                          ProgressBarState = Disabled }

        let agent = MailboxProcessor.Start(inboxHandler)
        agent.Error.Add(fun e ->
            let a = e
            raise a
        )
        agent

    let mutable modpackLink = String.Empty
    let mutable extractLocation = String.Empty
    let mutable mods = [{ Link = String.Empty; Name = String.Empty; Completed = false; ProjectId = 0 }]
    let mutable progressBarState = Disabled

    member this.StateAgent = updateLoop

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

            this.StateAgent.Post (UpdateProgress file.ProjectId)
        }