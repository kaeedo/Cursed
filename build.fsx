#r "./packages/FAKE/tools/FakeLib.dll"

open Fake
open Fake.AssemblyInfoFile

let buildDir = "./output/"

Target "SetVersion" (fun _ ->
    let releaseNotes = ReadFile "release-notes.md" |> ReleaseNotesHelper.parseReleaseNotes

    CreateFSharpAssemblyInfo ("." @@ "Cursed" @@ "AssemblyInfo.fs") [Attribute.Version releaseNotes.AssemblyVersion]
)

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
    ==> "SetVersion"
    ==> "Build"

RunTargetOrDefault "Build"
