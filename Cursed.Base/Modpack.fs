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

type ModpackManifest = JsonProvider<"./SampleManifest.json">

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
        |> run

    let extractZip location ((zipName: string), (zipLocation: string)) =
        let modpackSubdirectory = zipName.Substring(0, zipName.LastIndexOf('.'))
        let extractLocation = location @@ modpackSubdirectory
        ZipFile.ExtractToDirectory(zipLocation @@ zipName, extractLocation)

        let fileInfo = new FileInfo(zipLocation @@ zipName)
        fileInfo.Delete()

        modpackSubdirectory
    
    let getModDownloadRequest (file: ModpackManifest.File) =
        job {
            let response =
                Request.create Get (Uri <| sprintf "http://minecraft.curseforge.com/projects/%i" file.ProjectId)
                |> getResponse
                |> run

            let fileUrl = sprintf "%A/files/%i/download" response.responseUri file.FileId

            return Request.create Get (Uri fileUrl)
        }
    
    let downloadAllMods location =
        let manifestFile = File.ReadAllLines(location @@ "manifest.json") |> Seq.reduce (+)
        let manifest = ModpackManifest.Parse(manifestFile)
        
        let modDownloadRequests =
            manifest.Files.[0..4]
            |> List.ofSeq
            |> List.map (getModDownloadRequest)
            |> Job.conCollect
            |> run

        let a = 
            modDownloadRequests
            |> List.ofSeq
            |> List.map (fun r -> r |> getResponse |> run)
        a

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
                        this.ExtractLocation <- newState.ExtractLocation

                        return! messageLoop newState
                    | DownloadZip ->
                        let zipInformation = downloadZip oldState.ModpackLink oldState.ExtractLocation
                        let subdirectory = extractZip oldState.ExtractLocation zipInformation
                        
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
                            |> List.ofSeq
                        
                        let newState = { oldState with Mods = links}
                        this.Mods <- links

                        let a = downloadAllMods <| oldState.ExtractLocation @@ subdirectory

                        return! messageLoop oldState
                    | None -> ()
                }

            messageLoop { ModpackLink = String.Empty; ExtractLocation = String.Empty; Mods = [] }

        let agent = MailboxProcessor.Start(inboxHandler)
        agent.Error.Add(fun e ->
            let a = e
            raise a
        )
        agent

    let mutable extractLocation = String.Empty
    let mutable mods = [String.Empty, String.Empty]

    member this.StateAgent = updateLoop

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
        