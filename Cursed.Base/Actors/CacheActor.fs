namespace Cursed.Base

module CacheActor =
    open System

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

                        return! messageLoop newState
                    | GetCache reply ->
                        reply.Reply oldState
                        return! messageLoop oldState
                    | Load projects ->
                        return! messageLoop projects
                    | FileReplyMessage.Restart ->
                        return! messageLoop []

                    return! messageLoop oldState
                }

            messageLoop []

        MailboxProcessor.Start(inboxHandler)