﻿namespace Cursed.Base

open Common
open System
open System.IO
open System.Text
open System.Collections.Generic
open Newtonsoft.Json

module CacheActor =
    let private cacheFileLocation = HomePath @@ "cache.txt"

    let private ensureDirectory directoryPath =
        let directory = new DirectoryInfo(directoryPath)
        if not directory.Exists then
            directory.Create()

    let private ensureFile fileName =
        let file = new FileInfo(fileName)

        ensureDirectory <| file.DirectoryName

        if not file.Exists then
            let newFile = file.Create()
            newFile.Close()

    let FileLoop =
        let inboxHandler (inbox: MailboxProcessor<FileReplyMessage>) =
            let rec messageLoop (oldState: Cache) =
                async {
                    let! message = inbox.Receive()

                    match message with
                    | SaveProject project ->
                        let cachedProject =
                            oldState.Projects
                            |> List.tryFind (fun p ->
                                p.Id = project.Id
                            )

                        match cachedProject with
                        | Some _ -> return! messageLoop oldState
                        | None ->
                            let newState =
                                let projects = project :: oldState.Projects
                                { oldState with Projects = projects }
                            return! messageLoop newState
                    | SaveMod (projectId, modFile) ->
                        let project =
                            oldState.Projects
                            |> List.find (fun p ->
                                p.Id = projectId
                            )

                        if project.Files |> List.contains modFile then
                            return! messageLoop oldState

                        let projectWithAddedMod =
                            { project with
                                Files = modFile :: project.Files }

                        let newState =
                            let projects =
                                oldState.Projects
                                |> List.map (fun p ->
                                    if p = project then
                                        projectWithAddedMod
                                    else
                                        p
                                )
                            { oldState with Projects = projects }

                        File.WriteAllText(cacheFileLocation, JsonConvert.SerializeObject(newState), Encoding.UTF8)
                        return! messageLoop newState
                    | SaveVersionSkip version ->
                        let newState = { oldState with SkipVersion = version }
                        File.WriteAllText(cacheFileLocation, JsonConvert.SerializeObject(newState), Encoding.UTF8)
                        return! messageLoop newState
                    | SaveModpackLink link ->
                        let newState = { oldState with CurseLink = link }
                        File.WriteAllText(cacheFileLocation, JsonConvert.SerializeObject(newState), Encoding.UTF8)
                        return! messageLoop newState
                    | SaveModpackLocation location ->
                        let newState = { oldState with ModpackLocation = location }
                        File.WriteAllText(cacheFileLocation, JsonConvert.SerializeObject(newState), Encoding.UTF8)
                        return! messageLoop newState
                    | GetCache reply ->
                        reply.Reply oldState
                        return! messageLoop oldState
                    | Load ->
                        ensureFile cacheFileLocation
                        let cacheFile = File.ReadAllText(cacheFileLocation, Encoding.UTF8)

                        let getCache =
                            try
                                let cache = JsonConvert.DeserializeObject<Cache>(cacheFile)
                                match box cache with
                                | null ->
                                    let newCache = { Projects = []; SkipVersion = "0.0.0"; CurseLink = String.Empty; ModpackLocation = String.Empty }
                                    File.WriteAllText(cacheFileLocation, JsonConvert.SerializeObject(newCache), Encoding.UTF8)
                                    newCache
                                | _ ->
                                    cache
                            with
                            | _ ->
                                let projects = JsonConvert.DeserializeObject<List<Project>>(cacheFile)
                                if isNull projects then
                                    let migratedCache = { Projects = []; SkipVersion = "0.0.0"; CurseLink = String.Empty; ModpackLocation = String.Empty}
                                    File.WriteAllText(cacheFileLocation, JsonConvert.SerializeObject(migratedCache), Encoding.UTF8)
                                    migratedCache
                                else
                                    let migratedCache = { Projects = projects |> List.ofSeq; SkipVersion = "0.0.0"; CurseLink = String.Empty; ModpackLocation = String.Empty }
                                    File.WriteAllText(cacheFileLocation, JsonConvert.SerializeObject(migratedCache), Encoding.UTF8)
                                    migratedCache

                        return! messageLoop getCache
                    | FileReplyMessage.Restart ->
                        return! messageLoop { Projects = []; SkipVersion = "0.0.0"; CurseLink = String.Empty; ModpackLocation = String.Empty }

                    return! messageLoop oldState
                }

            messageLoop { Projects = []; SkipVersion = "0.0.0"; CurseLink = String.Empty; ModpackLocation = String.Empty }

        MailboxProcessor.Start(inboxHandler)
