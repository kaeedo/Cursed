namespace Cursed.Base.Tests

open Cursed.Base

open NUnit.Framework
open Swensen.Unquote

[<TestFixture>]
type MainFormControllerTests() = 
    [<Test>]
    member this.``When progress bar value is an int should return value``() = 
        let progress = Progress 12
        test <@ MainFormController.getProgress progress = 12 @>

    [<Test>]
    member this.``When progress is anything else should be 0`` () =
        test <@ MainFormController.getProgress Indeterminate = 0 @>
        test <@ MainFormController.getProgress Disabled = 0 @>