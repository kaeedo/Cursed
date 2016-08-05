namespace Cursed.Base

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