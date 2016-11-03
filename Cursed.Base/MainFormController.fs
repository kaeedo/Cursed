namespace Cursed.Base

module MainFormController =
    let getProgress state =
        match state with
        | Progress complete -> complete
        | _ -> 0

    let getCompletedMods mods =
        []
        |> Seq.cast<obj>
        (*
        mods
            |> Seq.filter (fun m ->
                m.Completed
            )
            |> Seq.map (fun m ->
                m.Name :> obj
            )
        *)