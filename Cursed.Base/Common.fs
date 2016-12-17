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