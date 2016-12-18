namespace Cursed.Base

module DataAccess =
    open Common
    open System.IO
    open System.Text
    open System.Collections.Generic
    open Newtonsoft.Json
    
    let private cacheFileLocation = HomePath @@ "cache.txt"

    let private ensureDirectory directoryPath =
        let directory = new DirectoryInfo(directoryPath)
        if not directory.Exists then
            directory.Create()

    let private ensureFile fileName =
        let file = new FileInfo(fileName)

        ensureDirectory <| file.DirectoryName

        if not file.Exists then
            let newFile = file.Create()
            newFile.Close()

    let Save cache =
        File.WriteAllText(cacheFileLocation, JsonConvert.SerializeObject(cache), Encoding.UTF8)

    let LoadCache () =
        ensureFile cacheFileLocation
        let cache = File.ReadAllText(cacheFileLocation, Encoding.UTF8)
        let projects = JsonConvert.DeserializeObject<IList<Project>>(cache)
        
        if isNull projects then
            CacheActor.FileLoop.Post <| Load []
        else
            CacheActor.FileLoop.Post <| Load (projects |> List.ofSeq)
