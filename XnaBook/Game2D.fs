namespace Playground

open System
open Microsoft.Xna.Framework
open Microsoft.Xna.Framework.Graphics
open Microsoft.Xna.Framework.Input
open Core

type Sprite =
    { pos : Vector2 ; speed : float32 ; txt : Texture2D ; size : Point ; offset : Point }
    member this.Draw (spriteBatch : SpriteBatch) =
        let sourceRect = Rectangle(this.offset, this.size)
        spriteBatch.Draw(this.txt, this.pos, Nullable.op_Implicit sourceRect, Color.White)

type Game2D () as this =
    inherit Game()
 
    let graphics = new GraphicsDeviceManager(this)
    let mutable spriteBatch = Unchecked.defaultof<_>
    let mutable playerSpriteSheet = Unchecked.defaultof<Texture2D>
    let mutable player = Unchecked.defaultof<Sprite>
    let mutable animCount = 0
    let mutable timeElapsed = 0.0

    do
        this.Content.RootDirectory <- "Content"
        this.IsMouseVisible <- true

    override this.Initialize() =
        base.Initialize()

    override this.LoadContent() =
        spriteBatch <- new SpriteBatch(this.GraphicsDevice)
        let list : int list = []
        let l = list.Length
        let l = List.length list
        playerSpriteSheet <- this.Content.Load<Texture2D>("bones")
        player <- {
            pos = Vector2.Zero
            speed = 206.f
            txt = playerSpriteSheet
            size = Point(64, 64)
            offset = Point(0, 128)
        }

    override this.Update (gameTime) =
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back = ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
        then this.Exit();

        let moveDir =
            let dir = getMovementDir (Keyboard.GetState())
            if dir <> Vector2.Zero then dir.Normalize()
            dir
        let newPos =
            let pos = player.pos + moveDir * player.speed * float32 gameTime.ElapsedGameTime.TotalSeconds

            let playerSize = player.size.ToVector2 ()

            let minClamp = Vector2.Zero

            let screenSize = Vector2(float32 this.GraphicsDevice.Viewport.Width,
                                     float32 this.GraphicsDevice.Viewport.Height)
            let maxClamp = screenSize - playerSize

            Vector2.Clamp(pos, minClamp, maxClamp)

        let newRow =
            let f n = n * 64 |> float32
            if moveDir.X > 0.0f then f 3
            else if moveDir.X < 0.0f then f 1
            else if moveDir.Y > 0.0f then f 2
            else if moveDir.Y < 0.0f then f 0
            else player.offset.Y |> float32
        let newCol =
            if moveDir.Length() > 0.0f then
                timeElapsed <- gameTime.ElapsedGameTime.TotalSeconds + timeElapsed

                let frameRate = 5.0 / 60.0
                if timeElapsed >= frameRate then
                    timeElapsed <- timeElapsed - frameRate
                    animCount <- animCount + 1 % 8
                animCount % 8
            else
                0

        let newOffset = Vector2(newCol * 64 |> float32, newRow).ToPoint()
        player <- { player with pos = newPos ; offset = newOffset}

        base.Update(gameTime)
 
    override this.Draw (gameTime) =
        this.GraphicsDevice.Clear Color.CornflowerBlue

        spriteBatch.Begin ()
        player.Draw(spriteBatch)
        spriteBatch.End ()

        base.Draw(gameTime)

