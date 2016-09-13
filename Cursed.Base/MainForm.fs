namespace Cursed.Base

open System
open System.Threading
open Eto.Forms
open Eto.Drawing

type MainForm(app: Application) = 
    inherit Form()
    let modpack = new Modpack(app)

    do 
        base.Title <- "Cursed"
        base.ClientSize <- new Size(900, 600)
        
        let layout = new TableLayout()

        let openSelectFolderButton = 
            let button = new Button(Text = "Extract Location")
            let selectFolderDialog = new SelectFolderDialog()
            let openSelectFolderHandler _ =
                selectFolderDialog.ShowDialog(layout.ParentWindow) |> ignore
            
            Observable.subscribe openSelectFolderHandler button.MouseDown |> ignore

            button
        
        let urlInputLabel = new Label(Text = "Curse Modpack URL")
        
        let urlInputTextBox = 
            let textBox = new TextBox()
            
            let onInput _ = 
                modpack.StateAgent.Post (UpdateLink textBox.Text)
            
            Observable.subscribe onInput textBox.TextChanged |> ignore

            textBox.TextBinding.BindDataContext<Modpack>((fun (m: Modpack) ->
                 m.Text
            ), DualBindingMode.OneWay) |> ignore

            textBox

        let discoverButton = 
            let button = new Button(Text = "Discover")

            let addModpackLinkHandler _ =
                modpack.StateAgent.Post DownloadZip
                //modpack.DownloadZip "https://minecraft.curseforge.com/projects/all-the-mods/" |> ignore

            Observable.subscribe addModpackLinkHandler button.MouseDown |> ignore

            button

        let urlInputRow = new TableRow([new TableCell(urlInputLabel); new TableCell(urlInputTextBox, true); new TableCell(discoverButton)])

        layout.Padding <- new Padding(10)
        layout.Spacing <- new Size(5, 5)
        layout.Rows.Add(new TableRow([new TableCell(openSelectFolderButton)]))
        layout.Rows.Add(urlInputRow)
        layout.Rows.Add(null)

        base.Content <- layout
        base.DataContext <- modpack

        // create a few commands that can be used for the menu and toolbar
        //let clickMe = new Command(MenuText = "Click Me!", ToolBarText = "Click Me!")
        //clickMe.Executed.Add(fun e -> ignore(MessageBox.Show(this, "I was clicked!")))

        let quitCommand = new Command(MenuText = "Quit")
        quitCommand.Shortcut <- Application.Instance.CommonModifier ||| Keys.Q
        quitCommand.Executed.Add(fun e -> Application.Instance.Quit())

        base.Menu <- new MenuBar()
        let fileItem = new ButtonMenuItem(Text = "&File")
        base.Menu.Items.Add(fileItem)

        base.Menu.ApplicationItems.Add(new ButtonMenuItem(Text = "&Preferences..."))
        base.Menu.QuitItem <- quitCommand.CreateMenuItem()
