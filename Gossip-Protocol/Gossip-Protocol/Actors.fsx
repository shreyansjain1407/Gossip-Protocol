#r "nuget: Akka.FSharp"

open System
open Akka.Actor
open Akka.FSharp
open Akka.Configuration


let system = System.create "system" <| Configuration.load()
let numActors = 5
//type WeightMessage = {s:float32; w:float32}


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


let gossipRef = spawn system "1" Gossiper
let gossipRef2 = spawn system "2" Gossiper

gossipRef <! (5.0,5.0)

system.WhenTerminated.Wait()