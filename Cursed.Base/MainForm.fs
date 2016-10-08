namespace Cursed.Base

open System
open System.IO
open Eto.Forms
open Eto.Drawing
open Operators
open Hopac

type MainForm(app: Application) = 
    inherit Form()
    let modpack = new Modpack(app)

    let extractLocationRow =
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
                modpack.StateAgent.Post (SetExtractLocation folderDialog.Directory)
            
            Observable.subscribe openSelectFolderHandler button.MouseDown |> ignore

            button
        new TableRow([new TableCell(extractLocationHelpText); new TableCell(extractLocationLabel); new TableCell(openSelectFolderButton)])

    let urlInputRow =
        let urlInputLabel = new Label(Text = "Curse Modpack URL")
        
        let urlInputTextBox = 
            let textBox = new TextBox()
            let onInput _ = 
                modpack.StateAgent.Post (UpdateModpackLink textBox.Text)
            
            Observable.subscribe onInput textBox.TextChanged |> ignore
            textBox

        let discoverButton = 
            let button = new Button(Text = "Discover")

            let addModpackLinkHandler _ =
                if String.IsNullOrWhiteSpace(modpack.ExtractLocation) then
                    MessageBox.Show("Please select an extract location first", MessageBoxType.Warning) |> ignore
                elif String.IsNullOrWhiteSpace(modpack.ModpackLink) then
                    MessageBox.Show("Please input the link to the Modpack", MessageBoxType.Warning) |> ignore
                else
                    async {
                        let! modpackLocation = modpack.StateAgent.PostAndAsyncReply DownloadZip

                        match modpackLocation with
                        | None -> MessageBox.Show("Something went wrong", MessageBoxType.Error) |> ignore
                        | Some ml ->
                            let manifestFile = File.ReadAllLines(ml @@ "manifest.json") |> Seq.reduce (+)
                            let manifest = ModpackManifest.Parse(manifestFile)
                    
                            manifest.Files.[0..2]
                            |> List.ofSeq
                            |> List.map (modpack.DownloadMod ml)
                            |> Job.conCollect
                            |> run
                            |> ignore
                    }
                    |> Async.Start

            Observable.subscribe addModpackLinkHandler button.MouseDown |> ignore
            button

        new TableRow([new TableCell(urlInputLabel); new TableCell(urlInputTextBox, true); new TableCell(discoverButton)])

    let progressBar =
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
        let progressBarMaxValueBinding = Binding.Property(fun (m: Modpack) -> m.Mods).Convert(fun mods ->
            mods.Length
        )
        progressBar.BindDataContext<int>(maxValueBinding, progressBarMaxValueBinding) |> ignore
        
        let progressBinding = Binding.Property(fun (pb: ProgressBar) -> pb.Value) 
        let progressBarProgressBinding = Binding.Property(fun (m: Modpack) -> m.ProgressBarState).Convert(fun state ->
            match state with
            | Progress percentComplete -> percentComplete
            | _ -> 0
        )
        progressBar.BindDataContext<int>(progressBinding, progressBarProgressBinding) |> ignore
        

        progressBar

    let incompleteModsListBox =
        let listBox = new ListBox()
        
        let dataStoreBinding = Binding.Property(fun (lb: ListBox) -> lb.DataStore) 
        let modsBinding = Binding.Property(fun (m: Modpack) -> m.Mods).Convert(fun mods ->
            mods
            |> Seq.filter (fun m ->
                not m.Completed
            )
            |> Seq.map (fun m ->
                m.Name :> obj
            )
        )
        listBox.BindDataContext<seq<obj>>(dataStoreBinding, modsBinding) |> ignore

        listBox.Height <- 500

        let layout = new DynamicLayout()
        layout.BeginVertical() |> ignore
        layout.Add(new Label(Text = "Incomplete")) |> ignore
        layout.Add(listBox) |> ignore
        layout

    let completeModsListBox =
        let listBox = new ListBox()
        
        let dataStoreBinding = Binding.Property(fun (lb: ListBox) -> lb.DataStore) 
        let modsBinding = Binding.Property(fun (m: Modpack) -> m.Mods).Convert(fun mods ->
            mods
            |> Seq.filter (fun m ->
                m.Completed
            )
            |> Seq.map (fun m ->
                m.Name :> obj
            )
        )
        listBox.BindDataContext<seq<obj>>(dataStoreBinding, modsBinding) |> ignore

        listBox.Height <- 500
        
        let layout = new DynamicLayout()
        layout.BeginVertical() |> ignore
        layout.Add(new Label(Text = "Complete")) |> ignore
        layout.Add(listBox) |> ignore
        layout

    do 
        base.Title <- "Cursed"
        base.ClientSize <- new Size(900, 600)

        let dynamicLayout =
            let layout = new DynamicLayout()
            layout.BeginVertical() |> ignore
            layout

        let modListsDynamicLayout =
            let layout = new DynamicLayout()
            layout.BeginHorizontal() |> ignore
            layout.Add(incompleteModsListBox, Nullable true) |> ignore
            layout.Add(completeModsListBox, Nullable true) |> ignore
            layout
        
        let tableLayout =
            let layout = new TableLayout()
        
            layout.Padding <- new Padding(10)
            layout.Spacing <- new Size(5, 5)
            layout.Rows.Add(extractLocationRow)
            layout.Rows.Add(urlInputRow)
            layout

        dynamicLayout.Add(tableLayout) |> ignore
        dynamicLayout.Add(progressBar) |> ignore
        dynamicLayout.Add(modListsDynamicLayout) |> ignore

        base.Content <- dynamicLayout
        base.DataContext <- modpack

        let quitCommand = new Command(MenuText = "Quit")
        quitCommand.Shortcut <- Application.Instance.CommonModifier ||| Keys.Q
        quitCommand.Executed.Add(fun e -> Application.Instance.Quit())

        base.Menu <- new MenuBar()
        let fileItem = new ButtonMenuItem(Text = "&File")
        base.Menu.Items.Add(fileItem)

        base.Menu.ApplicationItems.Add(new ButtonMenuItem(Text = "&Preferences..."))
        base.Menu.QuitItem <- quitCommand.CreateMenuItem()
