namespace Cursed.Base

open System
open Eto.Forms
open Eto.Drawing

type MainForm() = 
    inherit Form()
    let discoverButton = 
        let button = new Button(Text = "Discover")
        button.Click.Add(fun _ ->
            let modpack = new Modpack()
            modpack.DownloadZip "https://minecraft.curseforge.com/projects/all-the-mods" |> ignore
        )
        new TableCell(button)
    do 
        base.Title <- "Cursed"
        base.ClientSize <- new Size(900, 600)

        let layout = new TableLayout()
        
        let urlInputLabel = new TableCell(new Label(Text = "Curse Modpack URL"))
        let urlInputTextBox = new TableCell(new TextBox(), true)
        
        let urlInputRow = new TableRow([urlInputLabel; urlInputTextBox; discoverButton])

        layout.Padding <- new Padding(10)
        layout.Spacing <- new Size(5, 5)
        layout.Rows.Add(urlInputRow)
        layout.Rows.Add(null)

        base.Content <- layout

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
