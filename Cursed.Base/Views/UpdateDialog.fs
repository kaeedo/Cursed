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
                let title = new Label(Text = (sprintf "Update Available - Changes since %s" <| UpdateDialogController.GetCurrentVersion.ToString(3)))
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

                let textArea = new TextArea(Text = newNotes)
                textArea

            let actionButtonsRow =
                let updateButton = new Button(Text = "Download")
                let skipButton = new Button(Text = "Skip this version")
                let laterButton = new Button(Text = "Later")

                let tableRow = 
                    let row = new TableRow()
                    row.Cells.Add(new TableCell(updateButton))
                    row.Cells.Add(new TableCell(null, true))
                    row.Cells.Add(new TableCell(skipButton))
                    row.Cells.Add(new TableCell(laterButton))
                    row
                let tableLayout = new TableLayout()
                tableLayout.Spacing <- new Size(5, 0)
                tableLayout.Padding <- new Padding(0, 10, 0, 0)
                tableLayout.Rows.Add(tableRow)
                tableLayout

            layout.Padding <- Nullable <| new Padding(10)
            layout.Add(updateTitle) |> ignore
            layout.Add(releaseNotes, yscale = Nullable true) |> ignore
            layout.Add(actionButtonsRow) |> ignore
            layout

        base.Title <- "Update"
        base.ClientSize <- new Size(500, 500)
        base.Content <- layout
