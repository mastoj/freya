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

module Freya.Machine.Inspector

open Aether
open Chiron
open Freya.Inspector

(* Runtime *)

let private initialize =
    initializeFreyaMachineRecord

let private runtime =
    { Initialize = initialize }

(* Inspection *)

let private data =
    Lens.getPartial freyaMachineRecordPLens >> Option.map Json.serialize

let private inspection =
    { Data = data }

(* Inspector *)

let freyaMachineInspector =
    { Key = freyaMachineRecordKey
      Runtime = runtime
      Inspection = inspection }
