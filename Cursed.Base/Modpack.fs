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

    let downloadZip (link: string) location =
        job {
            let modpackLink = if link.EndsWith("/", StringComparison.OrdinalIgnoreCase) then link.Substring(0, link.Length) else link
            let fileUrl = modpackLink + "/files/latest"
        
            let homePath =
                match Environment.OSVersion.Platform with
                | PlatformID.Unix -> Environment.GetEnvironmentVariable("HOME")
                | PlatformID.MacOSX -> Environment.GetEnvironmentVariable("HOME")
                | _ -> Environment.GetFolderPath(Environment.SpecialFolder.Personal)

            let! response =
                Request.create Get (Uri fileUrl)
                |> getResponse

            use ms = new MemoryStream()
            response.body.CopyTo(ms)
            let bytes = ms.ToArray()

            //let zipName = Uri.UnescapeDataString(response.ResponseUrl.Split('/') |> Seq.last)
            let zipName = "zip"
            let zipLocation = Path.Combine([|homePath; ".cursedTemp"; zipName|])

            use file = File.Create(zipLocation)
            file.Write(bytes, 0, bytes.Length)

            let fileInfo = new FileInfo(zipLocation)
            fileInfo.Directory.Create()

            //using(File.Create(zipLocation)) (fun fs -> response.ResponseStream.CopyTo(fs))
            //File.WriteAllBytes(zipLocation, request)

            let extractLocation = Path.Combine[|location; zipName|]

            ZipFile.ExtractToDirectory(zipLocation, extractLocation)
            fileInfo.Delete()

            //REMOVE .zip FROM NAME
            //return zipName
        }
        |> run
    
    let downloadMod link location =
        async {
            let response = Http.RequestStream(link)

            let fileName = Uri.UnescapeDataString(response.ResponseUrl.Split('/') |> Seq.last)
            let fileLocation = Path.Combine([|location; fileName|])
            
            do! (using(File.Create(fileLocation)) (response.ResponseStream.CopyToAsync >> Async.AwaitTask))
        }

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
                        let zipName = downloadZip oldState.ModpackLink oldState.ExtractLocation
                        
                        (*let modlistHtml = Path.Combine([|oldState.ExtractLocation; "zip"; "modlist.html"|])

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
                        
                        links
                        |> Seq.map(fun l ->
                            downloadMod (snd l) (Path.Combine([|oldState.ExtractLocation; zipName|]))
                        )
                        |> Async.Parallel
                        |> Async.RunSynchronously
                        |> ignore*)

                        this.Mods <- []

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
        