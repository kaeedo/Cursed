﻿namespace Cursed.Base

open System
open System.Collections.ObjectModel
open System.ComponentModel
open Microsoft.FSharp.Quotations
open Microsoft.FSharp.Quotations.Patterns
open System.IO
open System.IO.Compression
open FSharp.Data

type State =
    { Link: string 
      ZipLocation: string }

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
    let mutable cursedState = { Link = ""; ZipLocation = "" }

    member this.UpdateState state =
        cursedState <- state
        this.OnPropertyChanged(<@ this.Text @>)

    member this.State
        with get() = cursedState

    member this.Text
        with get() = cursedState.ZipLocation

    member this.DownloadZip (link: string) =
        let modpackLink = if link.EndsWith("/", StringComparison.OrdinalIgnoreCase) then link.Substring(0, link.Length) else link
        let fileUrl = modpackLink + "/files/latest"
        
        (*async {
            let! response = Http.AsyncRequestStream(fileUrl)
            

            using (File.Create(@"C:/tmp/test.zip")) (fun fs -> response.ResponseStream.CopyTo(fs))
            
            ZipFile.ExtractToDirectory(@"C:/tmp/test.zip", @"C:/tmp/test")
        }
        |> Async.RunSynchronously
        |> ignore*)
        this.UpdateState { cursedState with State.ZipLocation = "wqdqwdq d" }
        ()