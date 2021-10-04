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
    | SetNodeCount of int
    | SetStart of int64
    | Rumor of String //For the gossip algorithm, this is the rumor sent
    | GossipTerminate of String
    | PushSumInit
    | PushSumMsg of Double * Double
    | PushSumTerminate

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
                    printfn "Gossip:\nStartTime: %i, FinishTime: %i, Difference: %i" start curTime (curTime - start)
                    Environment.Exit(0)
            
            |SetNodeCount count ->
                totalNodes <- count

            |SetStart time ->
                start <- time

            |PushSumTerminate ->
                let curTime = stopWatch.ElapsedMilliseconds
                terminatedNodes <- terminatedNodes + 1
                if terminatedNodes = totalNodes then
                    stopWatch.Stop()
                    printfn "PushSum:\n StartTime: %i, FinishTime: %i, Difference: %i" start curTime (curTime - start)
                    Environment.Exit(0)

            | _ -> ()

//This is the main actor that will be transmitting all the "Good Stuff"
type Node(processController: IActorRef, msg: int, designatedNum: int) =
    inherit Actor()
    let mutable msgCount = 0
    let mutable neighbour: IActorRef[] = [||] //This is the array thst contains all the neighbours of the current node
    let mutable totalNodes = 0
    let mutable terminated = false
    let mutable s = designatedNum |> float
    let mutable w = 1.
    let mutable rounds = 1
    let rLimit = 3
    let tLimit = 10
    let pushSumLimit = (10. ** -10.)

    override x.OnReceive(nodeMsg) =
        match nodeMsg :?> Message with
        |Initialization neighbourArr ->
            neighbour <- neighbourArr

        |Rumor str ->
            //Here the rumor is received by the actor and forwarded
            msgCount <- msgCount + 1
            if(msgCount = tLimit) then
                //Notifying the process controller that an actor has reached it's limit
                processController <! GossipTerminate("I have lived long enough, but you must continue")
            if (msgCount < (tLimit * neighbour.Length)) then
                neighbour.[System.Random().Next(0, neighbour.Length)] <! Rumor(str)
        
        |PushSumInit ->
            s <- s / 2.0
            w <- w / 2.0
            neighbour.[System.Random().Next(0, neighbour.Length)] <! PushSumMsg(s,w)

        |PushSumMsg (sReceived: double, wReceived: double) ->
            let sum = s + sReceived
            let weight = w + wReceived
            let diff = s/w - sum/weight |> abs

            if terminated then
                neighbour.[System.Random().Next(0, neighbour.Length)] <! PushSumMsg(sReceived,wReceived)
                ()
            if(rounds >= rLimit && not terminated) then
                terminated <- true
                processController <! PushSumTerminate
                ()
            else if (diff > pushSumLimit) then
                rounds <- 0
                s <- (s + sReceived)/2.
                w <- (w + wReceived)/2.
                neighbour.[System.Random().Next(0,neighbour.Length)] <! PushSumMsg(s,w)
            else
                rounds <- rounds + 1
                s <- s/2.
                w <- w/2.
                neighbour.[System.Random().Next(0,neighbour.Length)] <! PushSumMsg(s,w)
            
        | _ -> ()
        
    
//Node count is mutable because it may be changed during the exxecution of the program
let mutable nodeCount = int (string (fsi.CommandLineArgs.GetValue 1))
let topology = string (fsi.CommandLineArgs.GetValue 2)
let algo = string (fsi.CommandLineArgs.GetValue 3)
let tempx = float nodeCount
let system = ActorSystem.Create("System")
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
    printfn "Full topology"
    let actorArray = Array.zeroCreate( nodeCount + 1)
    //Loop to spawn actors
    for i in [0 .. nodeCount] do
        actorArray.[i] <- system.ActorOf(Props.Create(typeof<Node>, processController, 10, i+1), "ProcessController" + string i)
    //Loop to initialize the neighbours of spawned actors in this case all are neighnours
    for i in [0 .. nodeCount] do
        actorArray.[i] <! Initialization(actorArray)
    
    let baseActor = Random().Next(0, nodeCount)
    if algo = "gossip" then
        //Whenever an algorithm is started, we shall notify the process controller about
        //the number of nodes and the start time of the process
        processController <! SetNodeCount(nodeCount)
        stopWatch.Start()
        processController <! SetStart(stopWatch.ElapsedMilliseconds)

        //Initializing the first actor in the chain reaction
        actorArray.[baseActor] <! Rumor("This is some top secret info, do not disclose")
    else if algo = "pushsum" then
        stopWatch.Start()
        processController <! SetStart(stopWatch.ElapsedMilliseconds)
        actorArray.[baseActor] <! PushSumInit

| "2D" ->
    printfn "2D topology"
    let gridSide = int (ceil (sqrt (float nodeCount)))
    printfn "GridSize: %i" gridSide
    printfn "Number of Nodes: %i" (gridSide*gridSide)
    let actorArray = Array.zeroCreate(gridSide * gridSide)
    //Loop to spawn actors
    for i in [0 .. gridSide * gridSide - 1] do
        actorArray.[i] <- system.ActorOf(Props.Create(typeof<Node>, processController, 10, i+1), "ProcessController" + string i)
    //Loop to initialize the neighbours of spawned actors in this case all are neighnours
    for i in [0 .. gridSide - 1] do
        for j in [0 .. gridSide - 1] do
            let mutable neighbourArr: IActorRef [] = [||]
            if i > 0 then
                neighbourArr <- Array.append neighbourArr[|actorArray.[(i-1) * gridSide + j]|]
            if j < gridSide - 1 then
                neighbourArr <- Array.append neighbourArr[|actorArray.[(i * gridSide) + j + 1]|]
            if i < gridSide - 1 then
                neighbourArr <- Array.append neighbourArr[|actorArray.[(i+1) * gridSide + j]|]
            if j > 0 then
                neighbourArr <- Array.append neighbourArr[|actorArray.[(i * gridSide) + j - 1]|]
            
            actorArray.[i*gridSide + 1] <! Initialization(neighbourArr)

    let baseActor = Random().Next(0, nodeCount)
    if algo = "gossip" then
        //Whenever an algorithm is started, we shall notify the process controller about
        //the number of nodes and the start time of the process
        processController <! SetNodeCount(nodeCount)
        stopWatch.Start()
        processController <! SetStart(stopWatch.ElapsedMilliseconds)

        //Initializing the first actor in the chain reaction
        actorArray.[baseActor] <! Rumor("This is some top secret info, do not disclose")
    else if algo = "pushsum" then
        stopWatch.Start()
        processController <! SetStart(stopWatch.ElapsedMilliseconds)
        actorArray.[baseActor] <! PushSumInit
    
| "Line" ->
    printfn "Line topology"
    let actorArray = Array.zeroCreate( nodeCount + 1)
    //Loop to spawn actors
    for i in [0 .. nodeCount] do
        actorArray.[i] <- system.ActorOf(Props.Create(typeof<Node>, processController, 10, i+1), "ProcessController" + string i)
    //Loop to initialize the neighbours of spawned actors in this case all are neighnours
    for i in [0 .. nodeCount] do
        let mutable neighbourArr: IActorRef [] = [||]
        if i > 0 then
            neighbourArr <- Array.append neighbourArr[|actorArray.[i - 1]|]
        if i < nodeCount - 1 then
            neighbourArr <- Array.append neighbourArr[|actorArray.[i + 1]|]
            
        actorArray.[i] <! Initialization(actorArray)
    
    let baseActor = Random().Next(0, nodeCount)
    if algo = "gossip" then
        //Whenever an algorithm is started, we shall notify the process controller about
        //the number of nodes and the start time of the process
        processController <! SetNodeCount(nodeCount)
        stopWatch.Start()
        processController <! SetStart(stopWatch.ElapsedMilliseconds)

        //Initializing the first actor in the chain reaction
        actorArray.[baseActor] <! Rumor("This is some top secret info, do not disclose")
    else if algo = "pushsum" then
        stopWatch.Start()
        processController <! SetStart(stopWatch.ElapsedMilliseconds)
        actorArray.[baseActor] <! PushSumInit
    
| "imp3D" ->
    printfn "Improper 3D topology"
    let actorArray = Array.zeroCreate( nodeCount + 1)
    //Loop to spawn actors
    for i in [0 .. nodeCount] do
        actorArray.[i] <- system.ActorOf(Props.Create(typeof<Node>, processController, 10, i+1), "ProcessController" + string i)
    //Loop to initialize the neighbours of spawned actors in this case all are neighnours
    
    
    let baseActor = Random().Next(0, nodeCount)
    if algo = "gossip" then
        //Whenever an algorithm is started, we shall notify the process controller about
        //the number of nodes and the start time of the process
        processController <! SetNodeCount(nodeCount)
        stopWatch.Start()
        processController <! SetStart(stopWatch.ElapsedMilliseconds)

        //Initializing the first actor in the chain reaction
        actorArray.[baseActor] <! Rumor("This is some top secret info, do not disclose")
    else if algo = "pushsum" then
        stopWatch.Start()
        processController <! SetStart(stopWatch.ElapsedMilliseconds)
        actorArray.[baseActor] <! PushSumInit
        
| _ -> ()