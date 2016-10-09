#r "./packages/FAKE/tools/FakeLib.dll"

open Fake
open Fake.AssemblyInfoFile

let buildDir = "./build/"
let outputDir = "./output/"

Target "SetVersion" (fun _ ->
    let releaseNotes = ReadFile "release-notes.md" |> ReleaseNotesHelper.parseReleaseNotes

    CreateFSharpAssemblyInfo ("." @@ "Cursed.Base" @@ "AssemblyInfo.fs") [Attribute.Version releaseNotes.AssemblyVersion]
)

Target "Clean" (fun _ ->
    CleanDirs [ buildDir; outputDir ]
)

let mutable assemblies = [""]

Target "Build" (fun _ ->
    ensureDirectory buildDir

    assemblies <-
        ["./Cursed.sln"]
        |> MSBuildDebug buildDir "Build"
)

"Clean"
    ==> "SetVersion"
    ==> "Build"

RunTargetOrDefault "Build"
