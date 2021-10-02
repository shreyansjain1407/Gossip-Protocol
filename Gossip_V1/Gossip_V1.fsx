#time "on"
#r "nuget: Akka"
#r "nuget: Akka.FSharp"
#r "nuget: Akka.TestKit"

open System
open Akka.Actor
open Akka.Sharp
open Akka.Configuration

let stopWatch = System.Diagnostics.Stopwatch()

type Message = 
    | Init of IActorRef[]
    | Rumor of String //For the gossip algorithm
    | PushSum of String//Something
    | Gossip of String

type ActorX() = 
    inherit Actor()
    let mutable msgCount = 0
    let mutable start = 0L //This is the starting time
    let mutable totalNodes = 0

    override x.OnReceive(receivedMsg) =
        match receivedMsg :?> Message with
            |Rumor msg ->
                let curTime = stopWatch.ElapsedMilliseconds //This is the time at which the message was received
                msgCount <- msgCount + 1 //Incrementing Messages received
                if msgCount = totalNodes then
                    stopWatch.Stop()
                    printfn "StartTime: %i, FinishTime: %i, Difference: %i" start curTime (curTime - start)
                    Environment.Exit(0)
            
            |_ -> ()

type Node(nodeCount: IActorRef, msg: int, nodeNum: int) =
    inherit Actor()
    
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
    let actoorArray = Array.zeroCreate( nodeCount + 1)
    for i in [0 .. nodeCount] do
        actoorArray.[i] <- system.ActorOf(Props.Create(typeof<Node>, actorX, 10, i+1), )
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