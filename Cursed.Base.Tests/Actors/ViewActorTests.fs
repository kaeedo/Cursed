namespace Cursed.Base.Tests
open Cursed.Base
open System
open NUnit.Framework
open Swensen.Unquote

[<TestFixture>]
type ViewActorTests() = 
    [<TearDown>]
    member this.TearDown () =
        ViewActor.UpdateLoop.Post StateReplyMessage.Restart
    
    [<Test>]
    member this.``When updating modpack link should reply with the new link`` () = 
        let link = "Any fancy link"
        let updatedLink = ViewActor.UpdateLoop.PostAndReply (fun reply -> UpdateModpackLink (link, reply))
        test <@ updatedLink = link @>
    
    [<Test>]
    member this.``When setting extract location should reply with new location`` () = 
        let location = "any fancy extract location"
        let updatedLocation = ViewActor.UpdateLoop.PostAndReply (fun reply -> SetExtractLocation (location, reply))
        test <@ updatedLocation = location @>
    
    [<Test>]
    member this.``When adding mod should reply with updated list of mods`` () = 
        let mods =
            [ { Link = String.Empty; Name = "some name"; Completed = false; ProjectId = 1; }
              { Link = String.Empty; Name = "some other name"; Completed = false; ProjectId = 2; } ]
        mods 
        |> List.rev
        |> List.iter (fun m ->
            ViewActor.UpdateLoop.PostAndReply (fun reply -> AddMod (m.Name, m.ProjectId, reply)) |> ignore
        )

        let newMod = { Link = String.Empty; Name = "new name"; Completed = false; ProjectId = 3; }
        let updatedMods = ViewActor.UpdateLoop.PostAndReply (fun reply -> AddMod (newMod.Name, newMod.ProjectId, reply))

        let newMods = newMod :: mods
        test <@ updatedMods = newMods @>

    [<Test>]
    member this.``When updating modpack information should reply with mod count and progress bar state`` () = 
        let count = 4
        let updatedCount, updatedProgressBarState = ViewActor.UpdateLoop.PostAndReply (fun reply -> UpdateModpackInformation (count, reply))
        test <@ updatedCount = count @>
        test <@ updatedProgressBarState = Indeterminate @>

    [<Test>]
    member this.``When finishing download should reply with disabled`` () = 
        let updatedProgressBarState = ViewActor.UpdateLoop.PostAndReply FinishDownload
        test <@ updatedProgressBarState = Disabled @>

    [<Test>]
    member this.``When updating progress should reply with updated mod list`` () = 
        let mods =
            [ { Link = String.Empty; Name = "some name"; Completed = false; ProjectId = 1; }
              { Link = String.Empty; Name = "some other name"; Completed = false; ProjectId = 2; }
              { Link = String.Empty; Name = "new name"; Completed = false; ProjectId = 3; } ]
        mods 
        |> List.rev
        |> List.iter (fun m ->
            ViewActor.UpdateLoop.PostAndReply (fun reply -> AddMod (m.Name, m.ProjectId, reply)) |> ignore
        )

        let updatedProgressBarState, updatedMods = ViewActor.UpdateLoop.PostAndReply (fun reply -> UpdateProgress (1, reply))
        test <@ updatedProgressBarState = (ProgressBarState.Progress 1) @>
        let newMods =
            [ { Link = String.Empty; Name = "some name"; Completed = true; ProjectId = 1; }
              { Link = String.Empty; Name = "some other name"; Completed = false; ProjectId = 2; }
              { Link = String.Empty; Name = "new name"; Completed = false; ProjectId = 3; } ]
        test <@ updatedMods = newMods @>

        let updatedProgressBarState, updatedMods = ViewActor.UpdateLoop.PostAndReply (fun reply -> UpdateProgress (2, reply))
        test <@ updatedProgressBarState = (ProgressBarState.Progress 2) @>
        let newMods =
            [ { Link = String.Empty; Name = "some name"; Completed = true; ProjectId = 1; }
              { Link = String.Empty; Name = "some other name"; Completed = true; ProjectId = 2; }
              { Link = String.Empty; Name = "new name"; Completed = false; ProjectId = 3; } ]
        test <@ updatedMods = newMods @>