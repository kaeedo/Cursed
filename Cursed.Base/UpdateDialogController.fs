namespace Cursed.Base

open System
open System.Text.RegularExpressions
open System.Reflection
open Hopac
open HttpFs.Client

module UpdateDialogController =
    let private semverRegex = "(?:0|[1-9][0-9]*)\.(?:0|[1-9][0-9]*)\.(?:0|[1-9][0-9]*)(?:-[\da-zA-Z\-]+(?:\.[\da-zA-Z\-]+)*)?(?:\+[\da-zA-Z\-]+(?:\.[\da-zA-Z\-]+)*)?"
    
    let private getCurrentVersion =
        Assembly.GetExecutingAssembly().GetName().Version

    let GetReleaseNotes = 
        Request.create Get (Uri @"https://raw.githubusercontent.com/kaeedo/Cursed/master/release-notes.md")
        |> Request.responseAsString
        |> run

    let Versions =
        async {
            let latest =
                GetReleaseNotes.Split [|'\n'|]
                |> Array.head
                |> Regex.``match`` semverRegex

            let latestVersion =
                match latest with
                | None -> new Version("0.0.0")
                | Some matches ->
                    matches
                    |> Seq.cast<Group>
                    |> List.ofSeq
                    |> List.head
                    |> fun h -> new Version(h.Value)

            return latestVersion, getCurrentVersion
        }
    