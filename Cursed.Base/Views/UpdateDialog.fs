namespace Cursed.Base

open System
open Eto.Forms
open Eto.Drawing

type UpdateDialog() =
    inherit Dialog()

    do
        let layout = new DynamicLayout()

        base.Title <- "Update"
        base.ClientSize <- new Size(200, 200)
        base.Content <- layout
