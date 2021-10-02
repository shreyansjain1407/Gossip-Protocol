#time "on"
#r "nuget: Akka"
#r "nuget: Akka.FSharp"
#r "nuget: Akka.TestKit"

open System
open Akka.Actor
open Akka.FSharp
open Akka.Configuration

let stopWatch = System.Diagnostics.Stopwatch()

type Message = 
    | Init of IActorRef[]
    | Rumor of String //For the gossip algorithm
    | PushSum of String//Something
    | Gossip of String

type ActorX() = //Keeps the count of all actor related things
    inherit Actor()
    let mutable msgCount = 0
    let mutable start = 0L //This is the starting time
    let mutable totalNodes = 0

    override x.OnReceive(receivedMsg) =
        match receivedMsg :?> Message with
            | Rumor msg ->
                let curTime = stopWatch.ElapsedMilliseconds //This is the time at which the message was received
                msgCount <- msgCount + 1 //Incrementing Messages received
                if msgCount = totalNodes then
                    stopWatch.Stop()
                    printfn "StartTime: %i, FinishTime: %i, Difference: %i" start curTime (curTime - start)
                    Environment.Exit(0)
            
            | _ -> ()

//This is the main actor that will be transmitting all the "Good Stuff"
type Node(actorX: IActorRef, msg: int, Current_s: int) =
    inherit Actor()
    let mutable msgCount = 0
    let mutable neighbour: IActorRef[] = [||] //This is the array thst contains all the neighbours of the current node
    let mutable totalNodes = 0

    let mutable sCur = Current_s |> float
    let mutable wCur = 1.0
    let mutable rounds = 1
    let rLimit = 3
    let tLimit = 10

    override x.OnReceive(receivedMsg) =
        match receivedMsg with
        |Init neighbourArr ->
            neighbour <- neighbourArr
        
        | _ -> ()
        
    
//Node count is mutable because it may be changed during the exxecution of the program
let mutable nodeCount = int (string (fsi.CommandLineArgs.GetValue 1))
let topology = string (fsi.CommandLineArgs.GetValue 2)
let algo = string (fsi.CommandLineArgs.GetValue 2)
let tempx = float nodeCount

nodeCount = 
    if topology = "2D" || topology = "imp2D" then
        int (floor((tempx ** 0.5) ** 2.0))
    else
        int tempx


let actorX = system.ActorOf(Props.Create(typeof<ActorX>),"actorX")
//To be removed later
//https://getakka.net/api/Akka.Actor.ActorSystem.html
//https://getakka.net/api/Akka.Actor.Props.html

match topology with
| "full" -> 
    let actorArray = Array.zeroCreate( nodeCount + 1)
    for i in [0 .. nodeCount] do
        actorArray.[i] <- system.ActorOf(Props.Create(typeof<Node>, actorX, 10, i+1), "ActorX")
    for i in [0 .. nodeCount] do
        actorArray.[i] <- Init(actorArray)

    if algo = "gossip" then
        //Some stuff here
    else if algo = "pushsum" then
        //More stuff here


| "2D" -> 
    if algo = "gossip" then
        //Some stuff here
    else if algo = "pushsum" then
        //More stuff here


| "linee" -> 
    if algo = "gossip" then
        //Some stuff here
    else if algo = "pushsum" then
        //More stuff here


| "imp2D" -> 
    if algo = "gossip" then
        //Some stuff here
    else if algo = "pushsum" then
        //More stuff here


| _ -> 
    printfn "It is what it is"
    ()