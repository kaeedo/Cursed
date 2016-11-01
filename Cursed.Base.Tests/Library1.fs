namespace Cursed.Base.Tests

open NUnit.Framework
open Swensen.Unquote

[<TestFixture>]
type Class1() = 
    [<Test>]
    member this.``Fancy test``() = 
        test <@ 2 + 3 = 7 @>
