module Common

open System
open System.IO

let (@@) first second =
    Path.Combine([|first; second|])

let HomePath =
    let home = 
        match Environment.OSVersion.Platform with
        | PlatformID.Unix -> Environment.GetEnvironmentVariable("HOME")
        | PlatformID.MacOSX -> Environment.GetEnvironmentVariable("HOME")
        | _ -> Environment.GetFolderPath(Environment.SpecialFolder.Personal)
    home @@ ".cursedTemp"

type MaybeBuilder() =
    member this.Bind(x, f) =
        match x with
        | None -> None
        | Some a -> f a

    member this.Return(x) =
        Some x

let maybe = new MaybeBuilder()