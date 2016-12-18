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
                        return! messageLoop []
                    | SaveMod (projectId, modFile) ->
                        return! messageLoop []
                    | Load projects ->
                        return! messageLoop projects
                    | FileReplyMessage.Restart ->
                        return! messageLoop []

                    return! messageLoop oldState
                }

            messageLoop []

        MailboxProcessor.Start(inboxHandler)