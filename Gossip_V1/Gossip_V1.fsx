#r "nuget: Akka"
#r "nuget: Akka.FSharp"
#r "nuget: Akka.TestKit"

open System
open Akka.Actor
open Akka.FSharp

type Message = 
    | Init of IActorRef[]
    | Rumor of String //For the gossip algorithm
    | PushSum of String//Something


//Node count is mutable because it may be changed during the exxecution of the program
let mutable Node_Count = int (string (fsi.CommandLineArgs.GetValue 1))
let topology = string (fsi.CommandLineArgs.GetValue 2)
let algo = string (fsi.CommandLineArgs.GetValue 2)

match topology with
| "full" -> ()
| "2D" -> ()
| "linee" -> ()
| "imp2D" -> ()
| _ -> ()