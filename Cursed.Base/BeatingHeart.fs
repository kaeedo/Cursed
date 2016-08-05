//namespace Cursed.Base

module State =
    type S<'State, 'Value> =
        S of ('State -> 'Value * 'State)

    let runS (S f) state = f state

    let returnS x =
        let run state =
            x, state
        S run

    let bindS f xS = 
        let run state = 
            let x, newState = runS xS state
            runS (f x) newState
        S run

    type StateBuilder() =
        member this.Return(x) = returnS x
        member this.Bind(xS, f) = bindS f xS
        member this.ReturnFrom(xS) = xS

    let state = new StateBuilder()

    let getS =
        let run state =
            state, state
        S run

    let putS newState =
        let run _ =
            (), newState
        S run

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
