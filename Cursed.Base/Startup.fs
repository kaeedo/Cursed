namespace Cursed.Base

open FSharp.Data
open Hopac
open System
open System.Reflection



module Startup =
    let private getLatestVersion = 
        async {
            let html = new HtmlProvider<"https://kaeedo.github.io/Cursed/">()
            let version = html.Html.CssSelect("#latestRelease").Head.AttributeValue("value")

            return version
        }
    
    let private currentVersion =
        let version = Assembly.GetExecutingAssembly().GetName().Version
        new Version(version.Major, version.Minor, version.Build)

    let GetVersions =
        async {
            let! latest = getLatestVersion
            let latestVersion = new Version(latest)

            return currentVersion, latestVersion
        }
    