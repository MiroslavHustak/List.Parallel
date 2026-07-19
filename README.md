## Configuration Example

You can customize the degree of parallelism based on list size using an active pattern:

```fsharp
// Example values
let private myIdeaOfASmallList = 100
let private myIdeaOfALargelList = 200

let private maxDegreeOfParallelismThrottled = 12
let private maxDegreeOfParallelismMedium = 12
let private maxDegreeOfParallelism = 12

// Example code, just keep the signature
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
