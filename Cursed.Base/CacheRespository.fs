namespace Cursed.Base

open FSharp.Data.Sql
open System.Data.SQLite

type private Sql = SqlDataProvider<Common.DatabaseProviderTypes.SQLITE,
                                   SQLiteLibrary = Common.SQLiteLibrary.AutoSelect,
                                   ConnectionString = @"Data Source=C:\Users\Kai\Desktop\Cursed.db;Version=3;foreign keys=true">

module CacheRespository =
    let private ctx = Sql.GetDataContext()

