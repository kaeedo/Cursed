namespace Cursed.Base

module CacheActor =
    open Common
    open System.IO
    open System.Text
    open System.Collections.Generic
    open Newtonsoft.Json

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
            let rec messageLoop oldState =
                async {
                    let! message = inbox.Receive()

                    match message with
                    | SaveProject project ->
                        let cachedProject = 
                            oldState
                            |> List.tryFind (fun p ->
                                p.Id = project.Id
                            )

                        match cachedProject with
                        | Some _ -> return! messageLoop oldState
                        | None ->
                            let newState = project :: oldState
                            return! messageLoop newState

                    | SaveMod (projectId, modFile) ->
                        let project =
                            oldState
                            |> List.find (fun p ->
                                p.Id = projectId
                            )

                        if project.Files |> List.contains modFile then
                            return! messageLoop oldState

                        let projectWithAddedMod =
                            { project with
                                Files = modFile :: project.Files }

                        let newState =
                            oldState
                            |> List.map (fun p ->
                                if p = project then
                                    projectWithAddedMod
                                else
                                    p
                            )

                        File.WriteAllText(cacheFileLocation, JsonConvert.SerializeObject(newState), Encoding.UTF8) 
                        return! messageLoop newState
                    | GetCache reply ->
                        reply.Reply oldState
                        return! messageLoop oldState
                    | Load ->
                        ensureFile cacheFileLocation
                        let cache = File.ReadAllText(cacheFileLocation, Encoding.UTF8)
                        let projects = JsonConvert.DeserializeObject<IList<Project>>(cache)
        
                        if isNull projects then
                            return! messageLoop []
                        else
                            return! messageLoop (projects |> List.ofSeq)
                    | FileReplyMessage.Restart ->
                        return! messageLoop []

                    return! messageLoop oldState
                }

            messageLoop []

        MailboxProcessor.Start(inboxHandler)