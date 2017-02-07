namespace Cursed.Base

open System
open Eto.Forms
open Eto.Drawing

type UpdateDialog() =
    inherit Dialog()
    
    let semverRegex = "(?:0|[1-9][0-9]*)\.(?:0|[1-9][0-9]*)\.(?:0|[1-9][0-9]*)(?:-[\da-zA-Z\-]+(?:\.[\da-zA-Z\-]+)*)?(?:\+[\da-zA-Z\-]+(?:\.[\da-zA-Z\-]+)*)?"

    do
        let layout = 
            let layout = new DynamicLayout()
            let updateTitle =
                let title = new Label(Text = "Update Available")
                title.TextAlignment <- TextAlignment.Center
                title.Font <- new Font("Segoe UI", 14.0f)
                title

            let releaseNotes =
                let newNotes =
                    UpdateDialogController.GetReleaseNotes.Split [|'\n'|]
                    |> List.ofArray
                    |> List.takeWhile (fun line ->
                        not <| line.Contains(UpdateDialogController.GetCurrentVersion.ToString(3))
                    )
                    |> String.concat Environment.NewLine

                let textArea = new TextArea(Text=newNotes)
                textArea

            layout.Add(updateTitle) |> ignore
            layout.Add(releaseNotes) |> ignore
            layout

        base.Title <- "Update"
        base.ClientSize <- new Size(500, 500)
        base.Content <- layout
