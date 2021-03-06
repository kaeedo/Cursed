﻿namespace Cursed.Base

open System
open System.Reflection
open Eto.Forms
open Eto.Drawing

type MainForm(app: Application) =
    inherit Form()
    let modpack = new Modpack(app)

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
        layout.Rows.Add(MainView.ExtractLocationRow modpack app)
        layout.Rows.Add(MainView.UrlInputRow modpack app)
        layout

    let updateCheck =
        async {
            try
                let latest, current = UpdateDialogController.Versions |> Async.RunSynchronously

                let cache = CacheActor.FileLoop.PostAndReply GetCache
                let skipUpdateCheck =
                    let skipVersion = new Version(cache.SkipVersion)
                    skipVersion.CompareTo(latest) = 0

                let isLatest = latest.CompareTo(current) <= 0

                if (not isLatest) && (not skipUpdateCheck) then
                    app.Invoke (fun () ->
                        let update = new UpdateDialog()
                        app.MainForm.Title <- sprintf "Cursed - Update Available"

                        update.ShowModal()
                    )
            with
            | _ ->
                app.Invoke (fun () ->
                    app.MainForm.Title <- sprintf "Cursed - Update check failed"
                )
        }

    do
        dynamicLayout.Add(tableLayout) |> ignore
        dynamicLayout.Add(MainView.ProgressBar) |> ignore
        dynamicLayout.Add(modListsDynamicLayout) |> ignore

        base.Title <- "Cursed"
        base.Icon <- Icon.FromResource("icon.ico", Assembly.GetExecutingAssembly())
        base.ClientSize <- new Size(900, 600)
        base.Content <- dynamicLayout
        base.DataContext <- modpack

        updateCheck |> Async.Start
