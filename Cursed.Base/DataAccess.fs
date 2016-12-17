namespace Cursed.Base

module DataAccess =
    open Common
    open System.IO
    open System.Text
    open Newtonsoft.Json
    
    let CacheFileLocation = HomePath @@ "cache.txt"

    let EnsureDirectory directoryPath =
        let directory = new DirectoryInfo(directoryPath)
        if not directory.Exists then
            directory.Create()

    let EnsureFile fileName =
        let file = new FileInfo(fileName)

        EnsureDirectory <| file.DirectoryName

        if not file.Exists then
            let newFile = file.Create()
            newFile.Close()

    let Save cache =
        File.WriteAllText(CacheFileLocation, JsonConvert.SerializeObject(cache), Encoding.UTF8)

    let Load () =
        let cache = File.ReadAllText(CacheFileLocation, Encoding.UTF8)
        JsonConvert.DeserializeObject<Project list>(cache)