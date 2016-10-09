namespace Cursed.Base

open FSharp.Data
open System
open System.Reflection

type UpdateHelper() =
    let getLatestVersion = 
        async {
            let html = new HtmlProvider<"https://kaeedo.github.io/Cursed/">()
            let version = html.Html.CssSelect("#latestRelease").Head.AttributeValue("value")

            return version
        }
    
    let getCurrentVersion =
        Assembly.GetExecutingAssembly().GetName().Version

    member this.IsLatest =
        async {
            let! latest = getLatestVersion
            let latestVersion = new Version(latest)

            return latestVersion.CompareTo(getCurrentVersion) <= 0
        }