#r "packages/FAKE/tools/FakeLib.dll"

open Fake

let buildDir = "./output/"

Target "Clean" (fun _ ->
    CleanDirs [buildDir]
)

Target "Build" (fun _ ->
    MSBuildDebug buildDir "Build" ["./Cursed.sln"]
    |> Log "Output dir: "
)

"Clean"
    ==> "Build"

RunTargetOrDefault "Build"