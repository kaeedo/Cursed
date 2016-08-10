namespace Cursed.Base

open System

type Modpack() =
    member this.DownloadZip (url: string) = 
        let packUrl = if url.EndsWith("/") then url.Substring(0, url.Length - 1) else url
        0

    member this.Type (letter: string) =
        0