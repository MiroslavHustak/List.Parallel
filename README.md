//Examples
let private myIdeaOfASmallList = 100
let private myIdeaOfALargelList = 200

let private maxDegreeOfParallelismThrottled = 12
let private maxDegreeOfParallelismMedium = 12
let private maxDegreeOfParallelism = 12

//Example
let private maxDegreeOfParallelismExample : int -> int = 

    let (|Small|Medium|Large|) length = 
        match length with
        | length 
            when length < myIdeaOfASmallList 
            -> Small
        | length
            when length >= myIdeaOfASmallList && length <= myIdeaOfALargelList 
            -> Medium
        | _ -> Large
    
    function
        | Small  -> maxDegreeOfParallelismThrottled
        | Medium -> maxDegreeOfParallelismMedium
        | Large  -> maxDegreeOfParallelism
