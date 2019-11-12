module Doomer

open Microsoft.Xna.Framework
open Microsoft.Xna.Framework.Graphics
open Microsoft.Xna.Framework.Input

let map = [|
       1;1;1;1; 1;1;1;1; 1;1;1;1; 1;1;1;1;
       1;0;0;0; 0;1;0;0; 0;0;0;0; 0;0;0;1;
       1;0;0;0; 0;1;0;0; 0;0;0;0; 0;0;0;1;
       1;0;0;0; 0;1;0;0; 0;0;0;0; 0;0;0;1;

       1;0;0;0; 0;0;0;0; 0;0;0;0; 0;0;0;1;
       1;0;0;0; 0;0;0;0; 0;0;0;0; 0;0;0;1;
       1;0;0;0; 0;0;0;0; 0;0;0;0; 0;0;0;1;
       1;0;0;0; 0;0;0;0; 0;0;0;0; 0;0;0;1;

       1;0;0;0; 0;0;0;0; 0;0;0;0; 0;0;0;1;
       1;0;0;0; 0;0;0;0; 0;0;0;0; 0;0;0;1;
       1;0;0;0; 0;0;0;0; 0;0;0;0; 0;0;0;1;
       1;0;0;0; 0;0;0;0; 0;0;0;0; 0;0;0;1;

       1;0;0;0; 0;0;0;0; 0;0;0;0; 0;0;0;1;
       1;0;0;0; 0;0;0;0; 0;0;0;0; 0;0;0;1;
       1;0;0;0; 0;0;0;0; 0;0;0;0; 0;0;0;1;
       1;1;1;1; 1;1;1;1; 1;1;1;1; 1;1;1;1;
|]

type Doomer () as this =
    inherit Game()

    let graphics = new GraphicsDeviceManager(this)
    let mutable spriteBatch = Unchecked.defaultof<_>
    let mutable spriteFont = Unchecked.defaultof<_>
    let mutable hud = ""
    let mutable playerPos = Vector2(2.0f , 8.0f)
    let mutable playerRot = 0.0f
    let mutable walkSpeed = 5.0f
    let mutable rotSpeed = 2.5f
    let mutable playerDir = Vector2(0.0f, 1.0f)
    let mapWidth = 16
    let mapHeight = 16
    let depth = 16.0f
    let fov = 3.14159f / 4.0f
    let mmSize = 150
    let mutable plane = Vector2(0.0f, 0.66f)
    let mutable texture = Unchecked.defaultof<Texture2D>
    let mutable minimap = Unchecked.defaultof<Texture2D>
    let mutable colors = Unchecked.defaultof<Color array>
    let mutable mmColors = Unchecked.defaultof<Color array>
    let mutable screenWidth = 0
    let mutable screenHeight = 0

    let randomColors (cs : byref<_>) =
        let rand = new System.Random();
        cs <- Array.init (screenWidth * screenHeight) (fun i ->
            let r = rand.Next() * 255
            let g = rand.Next() * 255
            let b = rand.Next() * 255
            new Color(r, g, b))

    do
        this.Content.RootDirectory <- "Content"
        this.IsMouseVisible <- true

    override this.Initialize() =
        base.Initialize()

//        graphics.IsFullScreen <- true;
//        graphics.GraphicsProfile <- GraphicsProfile.HiDef;
        graphics.PreferredBackBufferWidth <- 1280
        graphics.PreferredBackBufferHeight <- 768
        graphics.ApplyChanges()

        screenWidth <- this.GraphicsDevice.Viewport.Width
        screenHeight <- this.GraphicsDevice.Viewport.Height

        texture <- new Texture2D(this.GraphicsDevice, screenWidth, screenHeight)
        minimap <- new Texture2D(this.GraphicsDevice, mmSize, mmSize)

        colors <- Array.create (screenWidth * screenHeight) Color.White
        mmColors <- Array.create (mmSize * mmSize) Color.White

    override this.LoadContent() =
        spriteBatch <- new SpriteBatch(this.GraphicsDevice)
        spriteFont <- this.Content.Load<SpriteFont>("DefaultFont")

    override this.Update (gameTime) =
        let dt = float32 gameTime.ElapsedGameTime.TotalSeconds
        let kbState = Keyboard.GetState()
        if (kbState.IsKeyDown(Keys.Escape))
            then this.Exit();

        let getKey key = if kbState.IsKeyDown(key) then 1.0f else 0.0f
        let ver = getKey Keys.W - getKey Keys.S
        let hor = getKey Keys.D - getKey Keys.A
        let rot = getKey Keys.L - getKey Keys.J

        playerRot <- playerRot + rot * rotSpeed * dt
        if ver > 0.0f then
            playerPos <- Vector2(playerPos.X + sin playerRot * walkSpeed * dt,
                                 playerPos.Y + cos playerRot * walkSpeed * dt)
        else if ver < 0.0f then
            playerPos <- Vector2(playerPos.X - sin playerRot * walkSpeed * dt,
                                 playerPos.Y - cos playerRot * walkSpeed * dt)
        if hor > 0.0f then
            playerPos <- Vector2(playerPos.X + cos playerRot * walkSpeed * dt,
                                 playerPos.Y - sin playerRot * walkSpeed * dt)
        else if hor < 0.0f then
            playerPos <- Vector2(playerPos.X - cos playerRot * walkSpeed * dt,
                                 playerPos.Y + sin playerRot * walkSpeed * dt)

//        hud <- sprintf "X : %s - Y : %s" (playerPos.X.ToString("0.00")) (playerPos.Y.ToString("0.00"))
        let screenWidthF = float32 screenWidth
        for x = 0 to screenWidth - 1 do
            let angle = (playerRot - fov / 2.0f) + (float32 x / screenWidthF) * fov;
            hud <- angle.ToString()

            let mutable distanceToWall = 0.0f
            let mutable hitWall = false

            let eye = Vector2(sin angle, cos angle)

            while not hitWall && distanceToWall < depth do
                distanceToWall <- distanceToWall + 0.9f

                let testX = playerPos.X + eye.X * distanceToWall |> int
                let testY = playerPos.Y + eye.Y * distanceToWall |> int

                if testX < 0 || testX >= mapWidth || testY < 0 || testY >= mapHeight then
                    hitWall <- true
                    distanceToWall <- depth
                else
                    if (map.[testY * mapWidth + testX] > 0) then
                        hitWall <- true

            let ceiling = float32 screenHeight * 0.5f - (float32 screenHeight / distanceToWall * 0.5f)
            let floor = float32 screenHeight - ceiling

            let darkest = 90.0f
            let lightest = 210.0f
            let wallShade = (1.0f - distanceToWall / depth) * (lightest - darkest ) + darkest |> int

//            hud <- wallShade.ToString()
            for y = 0 to screenHeight - 1 do
                let setColor color = colors.[y * screenWidth + x] <- color
                if y <= int ceiling then
                    setColor Color.SkyBlue
                else if y > int ceiling && y <= int floor then
                    setColor <| Color(wallShade, wallShade, wallShade)
                else
                    setColor Color.DarkOliveGreen

        // Minimap

        let mmPosX = playerPos.X / float32 mapWidth * float32 mmSize |> int
        let mmPosY = (1.0f - playerPos.Y / float32 mapHeight) * float32 mmSize |> int
        for i = 0 to mmColors.Length - 1 do mmColors.[i] <- Color.White
        mmColors.[mmPosY * mmSize + mmPosX] <- Color.Black

        mmColors.[(mmPosY + 1) * mmSize + mmPosX - 1] <- Color.Black
        mmColors.[(mmPosY - 1) * mmSize + mmPosX - 1] <- Color.Black
        mmColors.[(mmPosY) * mmSize + mmPosX - 1] <- Color.Black
        mmColors.[(mmPosY + 1) * mmSize + mmPosX + 1] <- Color.Black
        mmColors.[(mmPosY - 1) * mmSize + mmPosX + 1] <- Color.Black
        mmColors.[(mmPosY) * mmSize + mmPosX + 1] <- Color.Black
        mmColors.[(mmPosY - 1) * mmSize + mmPosX] <- Color.Black
        mmColors.[(mmPosY + 1) * mmSize + mmPosX] <- Color.Black

        base.Update(gameTime)

    override this.Draw (gameTime) =
        this.GraphicsDevice.Clear Color.CornflowerBlue

        spriteBatch.Begin()

        texture.SetData(colors)
        minimap.SetData(mmColors)
        spriteBatch.Draw(texture, Vector2.Zero, Color.White)
        spriteBatch.Draw(minimap, Vector2.One * 5.0f, Color.White)
        spriteBatch.DrawString(spriteFont, hud, Vector2(float32 screenWidth - 150.0f, 10.0f) , Color.Red)

        spriteBatch.End()

        base.Draw(gameTime)
