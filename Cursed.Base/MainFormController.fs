namespace Cursed.Base

open System.IO
open Hopac
open Common
open ModpackController

module MainFormController =
    let GetProgress state =
        match state with
        | Progress complete -> complete
        | _ -> 0

    let GetCompletedMods mods =
        mods
        |> Seq.filter (fun m ->
            m.Completed
        )
        |> Seq.map (fun m ->
            m.Name :> obj
        )

    let GetIncompleteMods mods =
        mods
        |> Seq.filter (fun m ->
            not m.Completed
        )
        |> Seq.map (fun m ->
            m.Name :> obj
        )

    let DownloadModpack (modpack: Modpack) =
        let modpackLocation = modpack.DownloadZip

        match modpackLocation with
        | Some location ->
            let manifestFile = File.ReadAllLines(location @@ "manifest.json") |> Seq.reduce (+)
            let manifest = ModpackManifest.Parse(manifestFile)
            let forge = CreateMultiMc location manifestFile

            manifest.Files.[..3]
            |> List.ofSeq
            |> List.map (modpack.DownloadMod location)
            |> Job.conCollect
            |> run
            |> ignore

            modpack.FinishDownload
            Some forge
        | None -> None
