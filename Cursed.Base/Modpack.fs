namespace Cursed.Base

open System
open System.Collections.ObjectModel
open System.ComponentModel
open Microsoft.FSharp.Quotations
open Microsoft.FSharp.Quotations.Patterns
open System.IO
open System.IO.Compression
open FSharp.Data

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

type Modpack() =
    inherit ModpackBase()

    let updateState state message =
        match message with
        | UrlInput t -> { state with UrlInput = t }
        | ExtractLocation t -> { state with ExtractLocation = t }
        | None -> state

    let inboxHandler (inbox: MailboxProcessor<StateUpdate>) =
        let rec messageLoop oldState = 
            async {
                match oldState with
                | NewState s ->
                    let! message = inbox.Receive()
                    let newState = updateState s message
                    return! messageLoop (NewState newState)
                | DownloadZip ->
                    inboxoldState
            }

        messageLoop (NewState { UrlInput = ""; ExtractLocation = "" })

    member this.StateAgent = 
        MailboxProcessor.Start(inboxHandler)

    member this.UpdateState state =
        this.OnPropertyChanged(<@ this.Text @>)

    member this.UrlInput
        with get() = cursedState.ExtractLocation

    member this.DownloadZip =
        let link = cursedState.UrlInput
        let modpackLink = if link.EndsWith("/", StringComparison.OrdinalIgnoreCase) then link.Substring(0, link.Length) else link
        let fileUrl = modpackLink + "/files/latest"
        
        (*async {
            let! response = Http.AsyncRequestStream(fileUrl)
            

            using (File.Create(@"C:/tmp/test.zip")) (fun fs -> response.ResponseStream.CopyTo(fs))
            
            ZipFile.ExtractToDirectory(@"C:/tmp/test.zip", @"C:/tmp/test")
        }
        |> Async.RunSynchronously
        |> ignore*)
        this.UpdateState { cursedState with AppState.ExtractLocation = "wqdqwdq d" }
        ()