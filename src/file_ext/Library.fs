namespace System.IO

open Result

module File =
    module Pdf =
        open iTextSharp.text.pdf
        open iTextSharp.text.pdf.parser

        let GetPages (path: string)  = 
            // choosing between lazy seq and eager array opted for array
            // since any mistake encountered while building this array
            // immediately translates to Error 
            // as partial processing is not an option here
            // i canoot process a file in its entirety
            // thus it should be labeled as Error and in the end discarded
            
            try
                [|
                    use doc = new PdfReader(path)     
                    for i in [1..doc.NumberOfPages] do
                        yield PdfTextExtractor.GetTextFromPage(doc, i, SimpleTextExtractionStrategy()) 
                |]
                |> Ok        

            // iTextSharp during this app's intergration tests at least once throwed an exception
            // other than IOException - which is the only one documented in source code to be thrown
            // so for safety reasons switched to catch-all here 
            // if i cannot process the file in question label it as error and report to user     
            with e ->
                path 
                |> Path.GetFileName
                |> sprintf "error encountered when processing %s" 
                |> Error
