namespace Cursed.Base

open FSharp.Data
open Hopac
open System
open System.Reflection



module Startup =
    let private getLatestVersion = 
        job {
            let html = new HtmlProvider<"https://kaeedo.github.io/Cursed/">()
            let version = html.Html.CssSelect("#latestRelease").Head.AttributeValue("value")

            return version
        }
    
    let private getCurrentVersion =
        Assembly.GetExecutingAssembly().GetName().Version

    

    let IsLatest =
        job {
            let! latest = getLatestVersion
            let latestVersion = new Version(latest)

            return latestVersion.CompareTo(getCurrentVersion) <= 0
        }
    