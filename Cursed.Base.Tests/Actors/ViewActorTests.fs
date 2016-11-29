namespace Cursed.Base.Tests
open Cursed.Base
open System
open NUnit.Framework
open Swensen.Unquote

[<TestFixture>]
type ViewActorTests() = 
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
