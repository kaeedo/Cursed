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
        let extractLocation = location @@ modpackSubdirectory @@ "minecraft"
        ZipFile.ExtractToDirectory(zipLocation @@ zipName, extractLocation)

        let fileInfo = new FileInfo(zipLocation @@ zipName)
        fileInfo.Delete()

        extractLocation
    
    let updateLoop =
        let inboxHandler (inbox: MailboxProcessor<StateMessage>) =
            let rec messageLoop oldState = 
                async {
                    let! message = inbox.Receive()
                    
                    match message with
                    | UpdateModpackLink link ->
                        let newState = { oldState with ModpackLink = link }
                        this.ModpackLink <- newState.ModpackLink

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

                            let manifestFile = File.ReadAllLines(subdirectory @@ "manifest.json") |> Seq.reduce (+)
                            let manifest = ModpackManifest.Parse(manifestFile)

                            let newState = { oldState with ModCount = manifest.Files.Length; ProgressBarState = Indeterminate }
                            this.ModCount <- newState.ModCount
                            this.ProgressBarState <- newState.ProgressBarState

                            directoryCopy (subdirectory @@ "overrides") subdirectory
                            reply.Reply (Some subdirectory)

                            return! messageLoop newState
                    | UpdateProgress projectId ->
                        let progress = UpdateProgressBarAmount oldState.ProgressBarState

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
                        this.ProgressBarState <- newState.ProgressBarState
                        this.Mods <- newState.Mods

                        return! messageLoop newState
                    | AddMod (modName, projectId) ->
                        let newState = { oldState with Mods = { Name = modName; Link = String.Empty; ProjectId = projectId; Completed = false } :: oldState.Mods }
                        this.Mods <- newState.Mods

                        return! messageLoop newState
                    | FinishDownload ->
                        this.ProgressBarState <- Disabled
                        return! messageLoop oldState
                }

            messageLoop { ModpackLink = String.Empty
                          ExtractLocation = String.Empty
                          Mods = []
                          ModCount = 0
                          ProgressBarState = Disabled }

        MailboxProcessor.Start(inboxHandler)

    let mutable modpackLink = String.Empty
    let mutable extractLocation = String.Empty
    let mutable mods = [{ Link = String.Empty; Name = String.Empty; Completed = false; ProjectId = 0 }]
    let mutable modCount = 0
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

            this.StateAgent.Post (AddMod (modName (), file.ProjectId))

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

    member this.CreateMultiMc location manifestFile =
        job {
            let directory = new DirectoryInfo(location)
            let outFile = new StreamWriter(directory.Parent.FullName @@ "instance.cfg")

            let manifest = ModpackManifest.Parse(manifestFile)
            let forge = manifest.Minecraft.ModLoaders.[0].Id

            CreateMultiMcInstance manifest.Version manifest.Name manifest.Author forge
            |> Seq.iter (fun setting ->
                outFile.WriteLine(sprintf "%s=%s" setting.Key setting.Value)
            )

            outFile.Flush()

            return forge
        }
        |> run