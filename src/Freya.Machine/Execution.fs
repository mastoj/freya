﻿//----------------------------------------------------------------------------
//
// Copyright (c) 2014
//
//    Ryan Riley (@panesofglass) and Andrew Cherry (@kolektiv)
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
//----------------------------------------------------------------------------

[<AutoOpen>]
module internal Freya.Machine.Execution

open Freya.Core
open Freya.Core.Operators
open Hekate

(* Errors

   Execution may (although should not) fail at runtime. Though
   the possibility of this for reasons captured by the type system
   should be small due to the verification system, we raise a specific
   error type in this instance. *)

exception ExecutionError of string

let private fail e =
    raise (ExecutionError e)

(* Operations

   Functions representing the execution and recording of monadic
   Machine operations, return the result of the operation when
   applicable (as in Binary operations). *)

let private record =
    addFreyaMachineExecutionRecord

let private start =
        record "start"
     *> Freya.init None

let private finish =
        record "finish"
     *> Freya.init ()

let private unary v operation =
        record v
     *> operation
     *> Freya.init None

let private binary v operation =
        record v
     *> operation
    >>= fun x -> Freya.init (Some (Edge x))

(* Execution

   Functions for executing against an execution graph, traversing the
   graph until either a Finish node is reached, or a node is
   unreachable, whether because the current node has no matching successors,
   or because the next node can't be found. *)

let private next v l : ExecutionGraph -> FreyaMachineNode option =
        Graph.successors v
     >> Option.bind (List.tryFind (fun (_, l') -> l = l'))
     >> Option.map fst

let private (|Start|_|) =
    function | Some (Start, _) -> Some (flip (next FreyaMachineNode.Start))
             | _ -> None

let private (|Finish|_|) =
    function | Some (Finish, _) -> Some ()
             | _ -> None

let private (|Unary|_|) =
    function | Some (Operation v, Some (Unary m)) -> Some (flip (next (Operation v)), v, m)
             | _ -> None

let private (|Binary|_|) =
    function | Some (Operation v, Some (Binary m)) -> Some (flip (next (Operation v)), v, m)
             | _ -> None

let execute exec =
    let rec eval node =
        freya {
            match node with
            | Some node ->
                match Graph.tryFindNode node exec with
                | Start (f) -> return! f exec <!> start >>= eval
                | Finish -> return! finish
                | Unary (f, v, m) -> return! f exec <!> unary v m >>= eval
                | Binary (f, v, m) -> return! f exec <!> binary v m >>= eval
                | _ -> fail (sprintf "Next Node %A Not Found" node)
            | _ ->
                fail (sprintf "Next Node %A Not Determined" node) }

    eval (Some Start)