namespace Cursed.Base

open System
open Eto.Forms
open Eto.Drawing

type UpdateDialog() =
    inherit Dialog()

    do
        let layout = 
            let layout = new DynamicLayout()
            let updateTitle =
                let title = new Label(Text = "Update Available")
                title.TextAlignment <- TextAlignment.Center
                title.Font <- new Font("Segoe UI", 14.0f)
                title

            let releaseNotes = 
                let textArea = new TextArea()
                textArea

            layout.Add(updateTitle) |> ignore
            layout.Add(releaseNotes) |> ignore
            layout

        base.Title <- "Update"
        base.ClientSize <- new Size(400, 450)
        base.Content <- layout
