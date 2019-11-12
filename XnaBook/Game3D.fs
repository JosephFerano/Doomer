namespace Playground

open System.Drawing
open Microsoft.Xna.Framework
open Microsoft.Xna.Framework.Graphics
open Microsoft.Xna.Framework.Input
open Core

type Game3D () as this =
    inherit Game()

    let graphics = new GraphicsDeviceManager(this)
    let mutable spriteBatch = Unchecked.defaultof<_>
    let mutable klimt : Texture2D = Unchecked.defaultof<Texture2D>

//    let mutable verts : VertexPositionColor array = [||]
    let mutable verts : VertexPositionTexture array = [||]
    let mutable vertexBuffer = Unchecked.defaultof<VertexBuffer>
    let mutable effect = Unchecked.defaultof<BasicEffect>
    let mutable trans = { pos = Vector3.Zero ; rot = 0.0f ; scale = Vector3.One };

    do
        this.Content.RootDirectory <- "Content"
        this.IsMouseVisible <- true

    override this.Initialize() =
        base.Initialize()

        this.Window.AllowUserResizing <- true

        verts <- [|
            VertexPositionTexture(Vector3(-1.0f, 1.0f,  0.0f), Vector2(0.0f, 0.0f))
            VertexPositionTexture(Vector3( 1.0f, 1.0f,  0.0f), Vector2(1.0f, 0.0f))
            VertexPositionTexture(Vector3(-1.0f,-1.0f,  0.0f), Vector2(0.0f, 1.0f))
            VertexPositionTexture(Vector3( 1.0f,-1.0f,  0.0f), Vector2(1.0f, 1.0f))
        |]

        vertexBuffer <- new VertexBuffer(this.GraphicsDevice,
                                         typeof<VertexPositionTexture>,
                                         Array.length verts, BufferUsage.None)
        vertexBuffer.SetData(verts)

//        effect.VertexColorEnabled <- true

    override this.LoadContent() =
        spriteBatch <- new SpriteBatch(this.GraphicsDevice)
        klimt <- this.Content.Load<Texture2D>("klimt")
        effect <- new BasicEffect(this.GraphicsDevice)
        effect.View <- Matrix.CreateLookAt(Vector3(0.0f, 0.0f, 5.0f), Vector3.Zero, Vector3.Up)
        effect.Projection <-
            Matrix.CreatePerspectiveFieldOfView(
                MathHelper.PiOver4, Core.screenRatio this, 1.0f, 100.0f)
        effect.Texture <- klimt
        effect.TextureEnabled <- true
        let rs = new RasterizerState()
//        rs.CullMode <- CullMode.None
        this.GraphicsDevice.RasterizerState <- rs

    override this.Update (gameTime) =
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back = ButtonState.Pressed
            || Keyboard.GetState().IsKeyDown(Keys.Escape))
        then this.Exit();

        if (Keyboard.GetState().IsKeyDown(Keys.Left)) then
            trans <- { trans with pos = trans.pos + Vector3 (-0.1f, 0.0f , 0.0f) }
        else if (Keyboard.GetState().IsKeyDown(Keys.Right)) then
            trans <- { trans with pos = trans.pos + Vector3 (0.1f, 0.0f , 0.0f) }
        if (Keyboard.GetState().IsKeyDown(Keys.Up)) then
            trans <- { trans with scale = trans.scale + Vector3 (-0.01f, -0.01f , -0.01f) }
        else if (Keyboard.GetState().IsKeyDown(Keys.Down)) then
            trans <- { trans with scale = trans.scale + Vector3 (0.01f, 0.01f , 0.01f) }

//        if (Keyboard.GetState().IsKeyDown(Keys.R)) then
//            trans <- { trans with rot = trans.rot + 0.01f }
        trans <- { trans with rot = trans.rot + 0.01f }

        base.Update(gameTime)
 
    override this.Draw (gameTime) =
        this.GraphicsDevice.Clear Color.CornflowerBlue

        this.GraphicsDevice.SetVertexBuffer(vertexBuffer)

        effect.World <- (Matrix.CreateScale trans.scale)
                        * (Matrix.CreateRotationZ trans.rot)
                        * (Matrix.CreateTranslation trans.pos)

        effect.CurrentTechnique.Passes
        |> Seq.iter (fun pass ->
            pass.Apply()
//            this.GraphicsDevice.DrawUserPrimitives<VertexPositionColor>(PrimitiveType.TriangleStrip, verts, 0, 1)
            this.GraphicsDevice.DrawUserPrimitives<VertexPositionTexture>(PrimitiveType.TriangleStrip, verts, 0, 2)
        )

        base.Draw(gameTime)

