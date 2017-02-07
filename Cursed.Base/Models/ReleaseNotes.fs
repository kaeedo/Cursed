namespace Cursed.Base

open System
open Eto.Forms

type ReleaseNotes(app: Application) as this =
    inherit NotifyPropertyChanged()

    let mutable releaseNotes = [ String.Empty ]

    //member this.SetReleaseNotes

    member this.ReleaseNotes
        with get() = releaseNotes
        and private set(value) =
            releaseNotes <- value
            app.Invoke (fun () -> this.OnPropertyChanged <@ this.ReleaseNotes @>)