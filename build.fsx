#r "./packages/FAKE/tools/FakeLib.dll"

open Fake
open Fake.Testing
open Fake.AssemblyInfoFile

open FSharp.Data

let buildDir = "." @@ "build"
let testDir = buildDir @@ "test"
let releaseNotes = ReadFile "release-notes.md" |> ReleaseNotesHelper.parseReleaseNotes

Target "SetVersion" (fun _ ->
    trace <| sprintf "Setting version %s" releaseNotes.AssemblyVersion
    CreateFSharpAssemblyInfo ("." @@ "Cursed.Base" @@ "AssemblyInfo.fs") [Attribute.Version releaseNotes.AssemblyVersion]
    CreateFSharpAssemblyInfo ("." @@ "Cursed" @@ "AssemblyInfo.fs") [Attribute.Version releaseNotes.AssemblyVersion]
)

Target "Clean" (fun _ ->
    CleanDirs [ testDir; buildDir ]
)

Target "Build" (fun _ ->
    ensureDirectory buildDir

    ["./Cursed/Cursed.fsproj"]
    |> MSBuildRelease buildDir "Build"
    |> ignore

    ["./Cursed.sln"]
    |> MSBuildDebug testDir "Build"
    |> ignore
)

Target "UnitTests" (fun _ ->
    !! (testDir + "/*.Tests.dll")
    |> NUnit3 id
)

Target "Pack" (fun _ ->
    let files =
        !! ("*.exe") ++ ("*.dll") ++ ("*.config")
        |> SetBaseDir buildDir

    CreateZipOfIncludes (sprintf "Cursed_%s.zip" releaseNotes.AssemblyVersion) "" 0 [ "", files ]
)

Target "Debug" (fun _ ->
    ensureDirectory buildDir

    ["./Cursed/Cursed.fsproj"]
    |> MSBuildDebug buildDir "Build"
    |> ignore
)

"Clean"
    ==> "SetVersion"
    ==> "Build"
    ==> "UnitTests"
    ==> "Pack"

RunTargetOrDefault "Pack"
