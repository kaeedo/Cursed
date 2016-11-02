#r "./packages/FAKE/tools/FakeLib.dll"

open Fake
open Fake.Testing
open Fake.AssemblyInfoFile

let buildDir = "./build/"

Target "SetVersion" (fun _ ->
    let releaseNotes = ReadFile "release-notes.md" |> ReleaseNotesHelper.parseReleaseNotes

    CreateFSharpAssemblyInfo ("." @@ "Cursed.Base" @@ "AssemblyInfo.fs") [Attribute.Version releaseNotes.AssemblyVersion]
)

Target "Clean" (fun _ ->
    CleanDirs [ buildDir ]
)

Target "Build" (fun _ ->
    ensureDirectory buildDir

    ["./Cursed.sln"]
    |> MSBuildDebug buildDir "Build"
    |> ignore
)

Target "UnitTests" (fun _ ->
    !! (buildDir + "/*.Tests.dll")
    |> NUnit3 id
)

"Clean"
    ==> "SetVersion"
    ==> "Build"
    ==> "UnitTests"

RunTargetOrDefault "UnitTests"
