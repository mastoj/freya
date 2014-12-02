﻿module Freya.Todo.Backend.Api

open System
open Freya.Core
open Freya.Core.Operators
open Freya.Inspector.Pipeline
open Freya.Machine
open Freya.Machine.Inspector
open Freya.Pipeline
open Freya.Pipeline.Operators
open Freya.Router
open Freya.Typed
open Freya.Todo.Backend.Storage

// Helpers

let inline represent _ x =
    { Metadata =
        { Charset = Some Charsets.UTF8
          Encodings = None
          MediaType = Some MediaTypes.JSON
          Languages = None }
      Data = toJSON x }

// Functions

let clearTodos =
    liftAsync (clear ())

let createTodo =
    returnM ()

let createdTodo n =
    represent n <!> returnM Array.empty<Todo>

let getTodos n =
    represent n <!> liftAsync (getAll ())

let todoProcessable =
    returnM true

let todoLastModified =
    returnM (DateTime.Now.Subtract (TimeSpan.FromDays 100.))

// Configuration

let jsonMediaType =
    returnM [ MediaTypes.JSON ]

let json =
    freyaMachine {
        mediaTypesSupported jsonMediaType }

let utf8Charset =
    returnM [ Charsets.UTF8 ]

let utf8 =
    freyaMachine {
        charsetsSupported utf8Charset }

// Resources

let todosMethods =
    returnM [ DELETE; GET; OPTIONS; POST ]

let todos =
    freyaMachine {
        including json
        including utf8

        doDelete clearTodos
        doPost createTodo

        handleCreated createdTodo
        handleOk getTodos

        lastModified todoLastModified
        methodsSupported todosMethods
        processable todoProcessable } |> compileFreyaMachine

let todo =
    freyaMachine {
        including json } |> compileFreyaMachine

// Routes

let api =
    freyaRouter {
        route All "/" todos
        route All "/:id" todo } |> compileFreyaRouter

// Pipeline

let config =
    { Path = "/inspect"
      Inspectors = 
        [ freyaMachineInspector ] }

let pipeline =
    freyaInspector config >?= api

// Katana

type TodoBackend () =
    member __.Configuration () =
        OwinAppFunc.fromFreya (pipeline)