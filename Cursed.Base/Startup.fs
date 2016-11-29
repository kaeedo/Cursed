namespace Cursed.Base

open FSharp.Data
open Hopac
open System.Data.SQLite
open System
open System.Reflection

type Startup() =
    let getLatestVersion = 
        job {
            let html = new HtmlProvider<"https://kaeedo.github.io/Cursed/">()
            let version = html.Html.CssSelect("#latestRelease").Head.AttributeValue("value")

            return version
        }
    
    let getCurrentVersion =
        Assembly.GetExecutingAssembly().GetName().Version

    member this.IsLatest =
        job {
            let! latest = getLatestVersion
            let latestVersion = new Version(latest)

            return latestVersion.CompareTo(getCurrentVersion) <= 0
        }

    member this.Create =
        SQLiteConnection.CreateFile(@"C:\Users\Kai\Desktop\Cursed.db")