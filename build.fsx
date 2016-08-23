#r "./packages/FAKE/tools/FakeLib.dll"

open Fake

let buildDir = "./output/"

Target "Clean" (fun _ ->
    CleanDirs [buildDir]
)

Target "Build" (fun _ ->
    ensureDirectory buildDir

    ["./Cursed.sln"]
    |> MSBuildDebug buildDir "Build"
    |> Log "Output dir: "
)

"Clean"
    ==> "Build"

RunTargetOrDefault "Build"