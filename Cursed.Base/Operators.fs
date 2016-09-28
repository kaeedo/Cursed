module Operators

open System.IO

let (@@) first second =
    Path.Combine([|first; second|])