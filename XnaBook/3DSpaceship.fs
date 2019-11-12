namespace Playground

open Microsoft.Xna.Framework
open Microsoft.Xna.Framework.Graphics
open Microsoft.Xna.Framework.Input

type Transform = { pos : Vector3 ; rot : float32 ; scale : Vector3 }

type Spaceship () as this =
    inherit Game()

    let graphics = new GraphicsDeviceManager(this)
    let mutable spriteBatch = Unchecked.defaultof<_>
    let mutable model = Unchecked.defaultof<Model>
    let mutable view = Matrix.Identity
    let mutable projection = Matrix.Identity
    let mutable vertexBuffer = Unchecked.defaultof<VertexBuffer>
    let mutable effect = Unchecked.defaultof<BasicEffect>
    let mutable trans = { pos = Vector3.Zero ; rot = 0.0f ; scale = Vector3.One };

    do
        this.Content.RootDirectory <- "Content"
        this.IsMouseVisible <- true

    override this.Initialize() =
        base.Initialize()

        this.Window.AllowUserResizing <- true


    override this.LoadContent() =
        spriteBatch <- new SpriteBatch(this.GraphicsDevice)

        model <- this.Content.Load<Model>("spaceship")

        effect <- new BasicEffect(this.GraphicsDevice)


        view <- Matrix.CreateLookAt(Vector3(0.0f, 0.0f, 25.0f), Vector3.Zero, Vector3.Up)
        projection <-
            Matrix.CreatePerspectiveFieldOfView(
                MathHelper.PiOver4, Core.screenRatio this, 1.0f, 300.0f)
        effect.TextureEnabled <- true

    override this.Update (gameTime) =
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back = ButtonState.Pressed
            || Keyboard.GetState().IsKeyDown(Keys.Escape))
        then this.Exit();

        if (Keyboard.GetState().IsKeyDown(Keys.Left)) then
            trans <- { trans with pos = trans.pos + Vector3.Left * 0.1f }
        else if (Keyboard.GetState().IsKeyDown(Keys.Right)) then
            trans <- { trans with pos = trans.pos + Vector3.Right * 0.1f}
        if (Keyboard.GetState().IsKeyDown(Keys.Up)) then
            trans <- { trans with scale = trans.scale - Vector3.One * 0.01f }
        else if (Keyboard.GetState().IsKeyDown(Keys.Down)) then
            trans <- { trans with scale = trans.scale + Vector3.One * 0.01f }

        trans <- { trans with rot = trans.rot + 0.01f }

        base.Update(gameTime)

    override this.Draw (gameTime) =
        this.GraphicsDevice.Clear Color.CornflowerBlue

        this.GraphicsDevice.SetVertexBuffer(vertexBuffer)

        let scale = Matrix.CreateScale trans.scale
        let rot = Matrix.CreateRotationY trans.rot
        let pos = Matrix.CreateTranslation trans.pos
        effect.World <- scale * rot * pos

        let transforms : Matrix array = Array.init model.Bones.Count (fun i  -> Matrix.Identity)
        model.CopyAbsoluteBoneTransformsTo transforms

        model.Meshes
        |> Seq.iter (fun mesh ->
            mesh.Effects
            |> Seq.iter (fun be ->
                let be = be :?> BasicEffect
                be.EnableDefaultLighting()
                be.View <- view
                be.Projection <- projection
//                printfn "%A" gameTime.TotalGameTime
                be.World <- rot * pos * mesh.ParentBone.Transform
            )
            mesh.Draw()
        )

        base.Draw(gameTime)

