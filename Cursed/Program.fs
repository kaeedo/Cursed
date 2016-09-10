namespace Cursed
module Program =

    open System
    open System.Threading
    open Cursed.Base

    [<EntryPoint>]
    [<STAThread>]
    let Main(args) = 
        let app = new Eto.Forms.Application(Eto.Platform.Detect)
        
        app.Run(new MainForm(app))
        0