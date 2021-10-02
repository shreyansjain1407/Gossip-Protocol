#time "on"
// #r "nuget: Akka"
#r "nuget: Akka.FSharp"
#r "nuget: Akka.TestKit"

open System
open Akka.Actor
open Akka.FSharp
open Akka.Configuration

type Message = 
    | Initialization of IActorRef []
    | Rumor of String //For the gossip algorithm, this is the rumor sent
    | GossipTerminate of String
    | PushSumMsg of Double * Double

let stopWatch = System.Diagnostics.Stopwatch()

//Keeps the count of all actor related things
type ProcessController() =
    inherit Actor()
    let mutable terminatedNodes = 0
    let mutable start = 0L //This is the starting time
    let mutable totalNodes = 0

    override x.OnReceive(receivedMsg) =
        match receivedMsg :?> Message with
            | GossipTerminate msg ->
                let curTime = stopWatch.ElapsedMilliseconds //This is the time at which the message was received
                terminatedNodes <- terminatedNodes + 1 //Incrementing Messages received
                if terminatedNodes = totalNodes then
                    stopWatch.Stop()
                    printfn "StartTime: %i, FinishTime: %i, Difference: %i" start curTime (curTime - start)
                    Environment.Exit(0)
            
            | _ -> ()

//This is the main actor that will be transmitting all the "Good Stuff"
type Node(processController: IActorRef, msg: int, designatedNum: int) =
    inherit Actor()
    let mutable msgCount = 0
    let mutable neighbour: IActorRef[] = [||] //This is the array thst contains all the neighbours of the current node
    let mutable totalNodes = 0

    let mutable s = designatedNum |> float
    let mutable w = 1.0
    let mutable rounds = 1
    let rLimit = 3
    let tLimit = 10

    override x.OnReceive(nodeMsg) =
        match nodeMsg :?> Message with
        |Initialization neighbourArr ->
            neighbour <- neighbourArr
        
        |Rumor str ->
            //Here the rumor is received by the actor and forwarded
            msgCount <- msgCount + 1
            
            if(msgCount = tLimit) then
                //Notifying the process controller that an actor has reached it's limit
                processController <! GossipTerminate()
                



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


let processController = system.ActorOf(Props.Create(typeof<ProcessController>),"processController")
//To be removed later
//https://getakka.net/api/Akka.Actor.ActorSystem.html
//https://getakka.net/api/Akka.Actor.Props.html

match topology with
| "full" -> 
    let actorArray = Array.zeroCreate( nodeCount + 1)
    //Loop to spawn actors
    for i in [0 .. nodeCount] do
        actorArray.[i] <- system.ActorOf(Props.Create(typeof<Node>, processController, 10, i+1), "ProcessController")
    //Loop to initialize the neighbours of spawned actors in this case all are neighnours
    for i in [0 .. nodeCount] do
        actorArray.[i] <- Initialization(actorArray)

    if algo = "gossip" then
        //Some stuff here
    else if algo = "pushsum" then
        //More stuff here


| "2D" -> 
    if algo = "gossip" then
        //Some stuff here
    else if algo = "pushsum" then
        //More stuff here


| "line" -> 
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