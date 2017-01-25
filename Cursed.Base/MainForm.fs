namespace Cursed.Base

open System
open Eto.Forms
open Eto.Drawing

type MainForm(app: Application) = 
    inherit Form()
    let modpack = new Modpack(app)

    do 
        DataAccess.LoadCache ()

        let dynamicLayout =
            let layout = new DynamicLayout()
            layout.BeginVertical() |> ignore
            layout

        let modListsDynamicLayout =
            let layout = new DynamicLayout()
            layout.BeginHorizontal() |> ignore
            layout.Add(MainView.IncompleteModsListBox, Nullable true) |> ignore
            layout.Add(MainView.CompleteModsListBox, Nullable true) |> ignore
            layout
        
        let tableLayout =
            let layout = new TableLayout()
        
            layout.Padding <- new Padding(10)
            layout.Spacing <- new Size(5, 5)
            //layout.Rows.Add(MainView.UpdateNotificationRow)
            layout.Rows.Add(MainView.ExtractLocationRow modpack app)
            layout.Rows.Add(MainView.UrlInputRow modpack app)
            layout

        dynamicLayout.Add(tableLayout) |> ignore
        dynamicLayout.Add(MainView.ProgressBar) |> ignore
        dynamicLayout.Add(modListsDynamicLayout) |> ignore

        base.Title <- "Cursed"
        base.ClientSize <- new Size(900, 600)
        base.Content <- dynamicLayout
        base.DataContext <- modpack

        async {
            let! current, latest = Startup.GetVersions
            let isLatest = latest.CompareTo(current) <= 0

            if not isLatest then
                modpack.SetUpdateAvailable (current, latest)
        }
        |> Async.Start
