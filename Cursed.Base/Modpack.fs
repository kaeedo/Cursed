namespace Cursed.Base

open System
open System.Text
//open HttpFs.Client

type State =
    { Link: string 
      ZipLocation: string }

type Modpack() =
    let mutable cursedState = { Link = ""; ZipLocation = "" }
    
    member this.UpdateState(state) =
        cursedState <- state

    member this.State
        with get() = cursedState

    member this.DownloadZip(link: string) =
        let modpackLink = if link.EndsWith("/", StringComparison.OrdinalIgnoreCase) then link.Substring(0, link.Length) else link
        let fileUrl = modpackLink + "/files/latest"
        (*let request =
            Request.createUrl Get fileUrl
            |> Request.autoFollowRedirectsDisabled
        let response =
            job {
                use! response = getResponse request
            }*)
        ()