namespace Cursed.Base

module UiState =
    open State

    type AppState = {
        Url: string
        Results: string list
    }

    let updateUrl newUrl =
        state {
            let! state = getS
            let newState = { state with Url = newUrl}
            do! putS newState
            return ()
        }

    let initState = { Url = "wefwe"; Results = ["qqq"; "www"] }

    let getValue stateM =
        runS stateM initState |> fst

    let setUrl =
        state {
            do! updateUrl "htttp://wefwef.qwcqwc.cqwcqc/"
        }

    let test1 = getS |> getValue
    let test2 = 
        state {
            do! setUrl
            return! getS
        }
        |> getValue
