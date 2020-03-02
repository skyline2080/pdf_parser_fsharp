// Learn more about F# at http://fsharp.org

open System
open System.IO

[<EntryPoint>]
let main argv =

    let secondsElapsed = 
        let sw = Diagnostics.Stopwatch()
        sw.Start()
        fun () ->
            sw.Stop()
            sw.Elapsed.TotalSeconds


    let sourcePath =
        if argv |> Array.isEmpty then
            printfn "pls specify the source directory path on the command line"
            Environment.Exit 1    
            
        let path = argv.[0]
        
        if path |> (Directory.Exists >> not) then
            printfn "the specified directory path does not exist"                     
            Environment.Exit 1
        
        path    

    let sourceFiles =
        let files =
            Directory.EnumerateFiles (sourcePath, "*.pdf", SearchOption.AllDirectories)
            |> Seq.toArray

        if files |> Array.isEmpty then
            printfn "specified path has no pdf files" 
            Environment.Exit 1

        files  

       
    let collectForEach = Array.Parallel.collect
    let reportError = Result.mapError (Console.WriteLine: string -> unit) // opting for Console for thread-safety reasons
    
    let sourcePages =
        sourceFiles 
        |> collectForEach ( 
                File.Pdf.GetPages
             >> reportError  
             >> function
                | Ok pageSeq -> // casting off Result wrapper for pages we could read        
                    pageSeq
                | Error _ ->    // for those we didn't, return an empty array          
                    Array.empty
            )
    
           
    let sourceItems =
        sourcePages
        |> collectForEach (
                PdfText.Regex.StripHeader 
             >> PdfText.Regex.StripFooter 
             >> PdfText.Regex.ParseFields)
    
    // checks for exceptional conditions which File.CreateText can possibly throw
    // were provided for in local var sourcePath evaluation         
    using 
        (new StreamWriter(Path.Combine(sourcePath, "output.csv"), false, Text.Encoding.Unicode))
        (fun fh -> sourceItems |> Seq.iter (fprintfn fh "%s"))    

    printfn "processed" 
    printfn "%d line items" (Seq.length sourceItems)
    printfn "%f seconds" (secondsElapsed())
        
    0

    

 