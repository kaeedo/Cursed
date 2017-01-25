namespace Cursed.Base

open System
open Eto.Forms

module MainView = 
    (*let UpdateNotificationRow =
        let updateCellLayout =
            let updateAvailableLabel = 
                let label = new Label(Text = "Update available")
                label.Font <- new Font("Seguo UI", 10.0f)
                label

            let versionsLabel = 
                let label = new Label()
                label.Text <- sprintf "Current: %A Latest: %A" "" ""
                label.TextAlignment <- TextAlignment.Center
                let textBinding = Binding.Property(fun (l: Label) -> l.Text)
                let versionsLabelTextBinding = Binding.Property(fun (m: Modpack) -> m.Versions).Convert(fun (current, latest) ->
                    sprintf "Current: %A\t\tLatest: %A" current latest
                )
                label.BindDataContext<string>(textBinding, versionsLabelTextBinding) |> ignore
                label

            let downloadButton = 
                let button = new Button(Text = "Get")
                let goToDownloads _ =
                    Process.Start(@"https://github.com/kaeedo/Cursed/releases/latest") |> ignore
            
                Observable.subscribe goToDownloads button.MouseDown |> ignore
                button
                    
            let row = new TableRow([new TableCell(updateAvailableLabel); new TableCell(versionsLabel, true); new TableCell(downloadButton)])
            
            let tableLayout =
                let layout = new TableLayout(row)
                let visibleBinding = Binding.Property(fun (tl: TableLayout) -> tl.Visible)
                let updateNotificationLayoutVisibleBinding = Binding.Property(fun (m: Modpack) -> m.Versions).Convert(fun (current, latest) -> latest.CompareTo(current) > 0)
                layout.BindDataContext(visibleBinding, updateNotificationLayoutVisibleBinding) |> ignore
                layout
            tableLayout

        let updateCell = new TableCell(updateCellLayout)
        new TableRow([new TableCell(); updateCell; new TableCell()])
    *)
    let ExtractLocationRow (modpack: Modpack) (app: Application) =
        let extractLocationHelpText = new Label(Text="Choose extract location")
        let extractLocationLabel = 
            let label = new Label()
            label.TextBinding.BindDataContext<Modpack>((fun (m: Modpack) ->
                m.ExtractLocation
            ), DualBindingMode.OneWay) |> ignore
            label
            
        let openSelectFolderButton = 
            let button = new Button(Text = "Extract Location")

            let openSelectFolderHandler _ =
                let folderDialog = new SelectFolderDialog()
                folderDialog.ShowDialog(app.Windows |> Seq.head) |> ignore
                modpack.SetExtractLocation folderDialog.Directory
            
            Observable.subscribe openSelectFolderHandler button.MouseDown |> ignore

            button
        new TableRow([new TableCell(extractLocationHelpText); new TableCell(extractLocationLabel); new TableCell(openSelectFolderButton)])

    let UrlInputRow (modpack: Modpack) (app: Application) =
        let urlInputLabel = new Label(Text = "Curse Modpack URL")
        
        let urlInputTextBox = 
            let textBox = new TextBox()
            let onInput _ = 
                modpack.UpdateModpackLink textBox.Text
            
            Observable.subscribe onInput textBox.TextChanged |> ignore
            textBox

        let downloadButton = 
            let button = new Button(Text = "Download")

            let addModpackLinkHandler _ =
                if String.IsNullOrWhiteSpace(modpack.ExtractLocation) then
                    MessageBox.Show("Please select an extract location first", MessageBoxType.Warning) |> ignore
                elif String.IsNullOrWhiteSpace(modpack.ModpackLink) then
                    MessageBox.Show("Please input the link to the Modpack", MessageBoxType.Warning) |> ignore
                else
                    async {
                        let forgeVersion = MainFormController.DownloadModpack modpack
                        match forgeVersion with
                        | Some forge -> app.Invoke (fun () -> MessageBox.Show(sprintf "To create a MultiMC instance, you must install Forge version: %s" forge, MessageBoxType.Information) |> ignore)
                        | None -> app.Invoke (fun () -> MessageBox.Show("Something went wrong", MessageBoxType.Error) |> ignore)
                    }
                    |> Async.Start

            Observable.subscribe addModpackLinkHandler button.MouseDown |> ignore
            button

        new TableRow([new TableCell(urlInputLabel); new TableCell(urlInputTextBox, true); new TableCell(downloadButton)])

    let ProgressBar =
        let progressBar = new ProgressBar()

        let enabledBinding = Binding.Property(fun (pb: ProgressBar) -> pb.Enabled) 
        let progressBarEnabledBinding = Binding.Property(fun (m: Modpack) -> m.ProgressBarState).Convert(fun state ->
            state <> Disabled
        )
        progressBar.BindDataContext<bool>(enabledBinding, progressBarEnabledBinding) |> ignore

        let indeterminateBinding = Binding.Property(fun (pb: ProgressBar) -> pb.Indeterminate) 
        let progressBarIndeterminateBinding = Binding.Property(fun (m: Modpack) -> m.ProgressBarState).Convert(fun state ->
            state = Indeterminate
        )
        progressBar.BindDataContext<bool>(indeterminateBinding, progressBarIndeterminateBinding) |> ignore

        let maxValueBinding = Binding.Property(fun (pb: ProgressBar) -> pb.MaxValue) 
        let progressBarMaxValueBinding = Binding.Property(fun (m: Modpack) -> m.ModCount)
        progressBar.BindDataContext<int>(maxValueBinding, progressBarMaxValueBinding) |> ignore
        
        let progressBinding = Binding.Property(fun (pb: ProgressBar) -> pb.Value) 
        let progressBarProgressBinding = Binding.Property(fun (m: Modpack) -> m.ProgressBarState).Convert(fun progress -> MainFormController.GetProgress progress)
        progressBar.BindDataContext<int>(progressBinding, progressBarProgressBinding) |> ignore
        

        progressBar

    let IncompleteModsListBox =
        let listBox = new ListBox()
        
        let dataStoreBinding = Binding.Property(fun (lb: ListBox) -> lb.DataStore) 
        let modsBinding = Binding.Property(fun (m: Modpack) -> m.Mods).Convert(fun mods -> MainFormController.GetIncompleteMods mods)
        listBox.BindDataContext<seq<obj>>(dataStoreBinding, modsBinding) |> ignore

        listBox.Height <- 500

        let layout = new DynamicLayout()
        layout.BeginVertical() |> ignore
        layout.Add(new Label(Text = "Incomplete")) |> ignore
        layout.Add(listBox) |> ignore
        layout

    let CompleteModsListBox =
        let listBox = new ListBox()
        
        let dataStoreBinding = Binding.Property(fun (lb: ListBox) -> lb.DataStore) 
        let modsBinding = Binding.Property(fun (m: Modpack) -> m.Mods).Convert(fun mods -> MainFormController.GetCompletedMods mods)
        listBox.BindDataContext<seq<obj>>(dataStoreBinding, modsBinding) |> ignore

        listBox.Height <- 500
        
        let layout = new DynamicLayout()
        layout.BeginVertical() |> ignore
        layout.Add(new Label(Text = "Complete")) |> ignore
        layout.Add(listBox) |> ignore
        layout