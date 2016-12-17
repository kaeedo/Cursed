namespace Cursed.Base

module DataAccess =
    open Common
    open System.IO
    open System.Text
    open Newtonsoft.Json
    
    let private cacheFileLocation = HomePath @@ "cache.txt"

    let Save cache =
        File.WriteAllText(cacheFileLocation, JsonConvert.SerializeObject(cache), Encoding.UTF8)

    let Load =
        let cache = File.ReadAllText(HomePath @@ "cache.txt", Encoding.UTF8)
        JsonConvert.DeserializeObject<Project list>(cache)
