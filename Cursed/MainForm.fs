namespace Cursed.Base

open System
open System.Windows
open Eto.Forms
open Eto.Drawing

type MainForm() = 
    inherit Form()
    let modpack = new Modpack()

    do 
        base.Title <- "Cursed"
        base.ClientSize <- new Size(900, 600)

        let layout = new TableLayout()
        
        let urlInputLabel = new Label(Text = "Curse Modpack URL")
        
        let urlInputTextBox = 
            let textBox = new TextBox()

            let onInput _ = 
                modpack.UpdateState { modpack.State with Link = textBox.Text }

            Observable.subscribe onInput textBox.TextChanged |> ignore

            textBox.TextBinding.BindDataContext<Modpack>(fun (m: Modpack) ->
                 m.Text
            ) |> ignore

            textBox

        let discoverButton = 
            let button = new Button(Text = "Discover")

            let addModpackLinkHander _ =
                modpack.DownloadZip "https://minecraft.curseforge.com/projects/all-the-mods/" |> ignore

            Observable.subscribe addModpackLinkHander button.MouseDown |> ignore

            button

        let urlInputRow = new TableRow([new TableCell(urlInputLabel); new TableCell(urlInputTextBox, true); new TableCell(discoverButton)])

        layout.Padding <- new Padding(10)
        layout.Spacing <- new Size(5, 5)
        layout.Rows.Add(urlInputRow)
        layout.Rows.Add(null)

        base.Content <- layout

        base.DataContext <- modpack

        let quitCommand = new Command(MenuText = "Quit")
        quitCommand.Shortcut <- Application.Instance.CommonModifier ||| Keys.Q
        quitCommand.Executed.Add(fun e -> Application.Instance.Quit())

        base.Menu <- new MenuBar()
        let fileItem = new ButtonMenuItem(Text = "&File")
        base.Menu.Items.Add(fileItem)

        base.Menu.ApplicationItems.Add(new ButtonMenuItem(Text = "&Preferences..."))
        base.Menu.QuitItem <- quitCommand.CreateMenuItem()
