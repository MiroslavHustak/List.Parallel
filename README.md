**Package Manager Console in Visual Studio (PM>):**

`NuGet\Install-Package List.Parallel -Version 1.0.2`

**.NET CLI:**

`dotnet add package List.Parallel --version 1.0.2`

**Paket CLI:**

`paket add List.Parallel --version 1.0.2`

**PackageReference:**

`<PackageReference Include="List.Parallel" Version="1.0.2" />`




### Usage:

See library.fs - the very simple F# code will tell you all you need to use this library. You can also copy the code to create your own custom variant. 


### Legend:

**Examples:**
`List.Parallel.iter_CPU_AW_Async`
`List.Parallel.iter2_IO_AW_Token_Async`
`List.Parallel.iter2_CPU_PT`

**Parallelism mechanism:**

PT  -> Task Parallel Library (TPL)-based. Underlying mechanism: TPL's Parallel.For/ForEach via Array.Parallel

AW  -> Async Workflow-based. Underlying mechanism: F# async workflows composed via Async.Parallel


**Workload / behavior modifiers:**

CPU  -> CPU-bound operations

IO  -> I/O-bound operations

Token  -> variant with built-in cancellation support (via CancellationToken)

Async  -> asynchronous variant
