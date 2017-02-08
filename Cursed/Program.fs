namespace Cursed

module Program =
    open System
    open System.Threading
    open Cursed.Base

    [<EntryPoint>]
    [<STAThread>]
    let Main(args) = 
        if Eto.EtoEnvironment.Platform.IsWindows then
            let app = new Eto.Forms.Application(new Eto.Wpf.Platform())
            app.Run(new MainForm(app))
        else
            let app = new Eto.Forms.Application(new Eto.GtkSharp.Platform())
            app.Run(new MainForm(app))
        0