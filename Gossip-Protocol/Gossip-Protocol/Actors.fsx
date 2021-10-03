#r "nuget: Akka.FSharp"

open System
open Akka.Actor
open Akka.FSharp
open Akka.Configuration


let system = System.create "system" <| Configuration.load()
//let numActors = 5
//type WeightMessage = {s:float32; w:float32}

type GossipMessage =
    | Rumor of rumor : string
    | Send of rumor : string

type GodMessage = 
    | Init of numActors : int * rumor : string
    | Time of numActors : int * rumor : string

type Index = 
    {
        x : int
        y : int
        z : int
    }
let rand = new Random()
(*
let Gossiper (mailbox : Actor<_>) = 
    let si = 1.0
    let wi = 1.0
    let mutable s = si
    let mutable w = wi
    let mutable ratio = s/w 
    let rec loop() = actor {
        let! (incS, incW) = mailbox.Receive()
        let send = mailbox.Sender()
        s <- s + incS
        w <- w + incW
        printfn "s: %f, w: %f" s w
        send <! (s/2.0, w/2.0)
        return! loop()
    } loop()
*)

let inline charToInt c = int c - int '0'

// only ready for line
let neighborSelector(index : int) = 
    let mutable random = rand.Next(0,1)
    if (random = 0) then
        random <- -1
    let mutable neighbor = index + random
    if(neighbor <= 0) then
        neighbor <- 1
    if(neighbor > 5) then
        neighbor <- 5 //implement upper limit for actors
    neighbor


let Gossiper (mailbox : Actor<_>) = 
    let mutable m_rumor = ""
    let mutable m_listened = 0

    let rec loop() = actor {
        let! (msg : GossipMessage) = mailbox.Receive()
        let send = mailbox.Sender()
        match msg with
            | Rumor(rumor) ->
                if(m_rumor.Equals "") then
                    m_rumor <- rumor
                else
                    m_listened <- m_listened + 1
                let name = mailbox.Self.Path.Name
                printf "%s : %s : %i\n" name rumor m_listened
                ()
            | Send(rumor) ->
                
                let name = mailbox.Self.Path.Name
                let node = name.[name.Length - 1] |> charToInt
                let neighbor = neighborSelector node
                let neighborPath = "akka://system/user/Gossiper_" + string neighbor
                select neighborPath system <! Rumor(rumor)
        return! loop()
    } loop()



let God (mailbox : Actor<_>) =
    let rec loop() = actor {
        let! (msg : GodMessage) = mailbox.Receive()

        match msg with
            | Init(numActors, rumor) ->
                let gossipPool = [1 .. numActors] |> List.map(fun workerID -> spawn system ("Gossiper_" + string workerID) Gossiper)
                gossipPool.[0] <! Rumor(rumor)
            | Time(numActors, rumor) ->
                for id in 1 .. numActors do 
                    let gossipPath = "akka://system/user/Gossiper_" + string id
                    select gossipPath system <! Send(rumor)
                
        return! loop()
    } loop()

//let gossipRef = spawn system "1" Gossiper
//let gossipRef2 = spawn system "2" Gossiper
let god = spawn system "god" God

let Timer (mailbox : Actor<_>) = 
    let timer = new Timers.Timer(1000.)
    let rec loop() = actor {
        let! (numActors, rumor) = mailbox.Receive()

        Async.Sleep(1000)
        god <! Time(numActors, rumor)
        return! loop()
    } loop()

let timer = spawn system "timer" Timer

god <! Init(5, "rumor")
//timer <! (5, "rumor")
while true do
    timer <! (5, "rumor")
    
//gossipRef <! (5.0,5.0)

system.WhenTerminated.Wait()