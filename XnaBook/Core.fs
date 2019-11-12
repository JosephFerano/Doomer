module Playground.Core

open Microsoft.Xna.Framework
open Microsoft.Xna.Framework.Graphics
open Microsoft.Xna.Framework.Input

let screenRatio (game : Game) = float32 game.Window.ClientBounds.Width / float32 game.Window.ClientBounds.Height

let (|KeyDown|_|) k (state : KeyboardState) = if state.IsKeyDown k then Some() else None

let getMovementDir = function
    | KeyDown Keys.W & KeyDown Keys.A -> Vector2(-1.f, -1.f)
    | KeyDown Keys.W & KeyDown Keys.D -> Vector2(1.f, -1.f)
    | KeyDown Keys.S & KeyDown Keys.A -> Vector2(-1.f, 1.f)
    | KeyDown Keys.S & KeyDown Keys.D -> Vector2(1.f, 1.f)
    | KeyDown Keys.W -> Vector2(0.f, -1.f)
    | KeyDown Keys.A -> Vector2(-1.f, 0.f)
    | KeyDown Keys.S -> Vector2(0.f, 1.f)
    | KeyDown Keys.D -> Vector2(1.f, 0.f)
    | _ -> Vector2.Zero

type Transform = { pos : Vector3 ; rot : float32 ; scale : Vector3 }

