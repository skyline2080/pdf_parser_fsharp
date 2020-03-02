namespace System

open System.Text.RegularExpressions

module PdfText =
    module Regex =
        let inline private removeWhitespaces (s: string) =
            Regex.Replace(s, @"\s+", "")    
        
        let inline private removeNewlines (s: string) =
            s.Replace("\n", "")     


        let StripHeader =
            let patHeader = @"^(.*\n){1,}(А Б )?1 1а 2 2а 3 4 5 6 7 8 9 10 10а 11"
            let re = Regex(patHeader, RegexOptions.Compiled)
            fun s -> 
                re.Replace(s, "\n")
        
        let StripFooter =    
            let patFooter = @"Всего к оплате(.*\n)+(.*)$"
            let re = Regex(patFooter, RegexOptions.Compiled)
            fun s -> 
                re.Replace(s, "\n")

        let ParseFields =
            let pat = 
                @"[\d]+ (?<desc>(.*\n)+?)" +                // item description                
                @"(?<misc>[\d]{10}.*?)" +                   // tariff code + pricing
                @"(?<coo>[\w\s]+)" +                        // country of origin
                @"(?<cust>[\d]{8}/[\d]{6}/[\d]+\n[\d]*)"    // customs declaration
            
            let patMisc =
                @"(?<tariffcode>[\d]{10}) (?<measureunitcode>[\d]{3}) (?<measureunit>[\w]+) (?<pricing>.*?) без акциза .*?"
                    
            let patPricing =
                @"^(?<units>[\d]{1,3}( \d\d\d)*(?!,))(?<unitprice>.*?,[\d]{2}) (?<itemtotal>[\d\s]+,[\d]{2})$"

            let patCoo = 
                @".*?(?<cooname>[а-яА-Я].*)"
                
            let reItemLine = Regex(pat, RegexOptions.Compiled)
            let reMisc = Regex(patMisc, RegexOptions.Compiled)
            let rePricing = Regex(patPricing, RegexOptions.Compiled)
            let reCoo = Regex(patCoo, RegexOptions.Compiled ||| RegexOptions.Singleline)    

            fun s -> 
                [|
                    for m in reItemLine.Matches(s) do
                        let desc = m.Groups.["desc"].Value |> removeNewlines
                        let cust = m.Groups.["cust"].Value |> removeNewlines

                        let miscmatch = reMisc.Match(m.Groups.["misc"].Value)

                        let tariffcode = miscmatch.Groups.["tariffcode"].Value
                        let measureunitcode = miscmatch.Groups.["measureunitcode"].Value
                        let measureunit = miscmatch.Groups.["measureunit"].Value
                        
                        let coo = 
                            reCoo.Match(
                                m.Groups.["coo"].Value
                            ).Groups.["cooname"].Value    |> removeNewlines

                            
                        
                        let pricingmatch = 
                            rePricing.Match(
                                miscmatch.Groups.["pricing"].Value)

                            

                        let units = pricingmatch.Groups.["units"].Value         |> removeWhitespaces
                        let unitprice = pricingmatch.Groups.["unitprice"].Value |> removeWhitespaces
                        let itemtotal = pricingmatch.Groups.["itemtotal"].Value |> removeWhitespaces

                        [|
                            tariffcode
                            desc
                            coo
                            units
                            measureunit
                            measureunitcode
                            unitprice
                            itemtotal
                            cust 
                        |]
                        |> String.concat "\t" 
                |]