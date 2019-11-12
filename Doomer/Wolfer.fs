module Wolfer

open System.Collections.Generic
open Microsoft.Xna.Framework
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

// TODO:
// 1.) Load map from file, this will allow spaces to indicate empty space, should make things easier
// 2.) Texture blitting can be optimized in several ways;
//         - Rotate canvas 90 clockwise, then fill in by rows, rather than by column, then roate in draw
//         - Either parallelize the whole raycasting routine, or the blitting loop
//         - Have 2-3 color buffers and swap them, in case GPU is still processing and blocking as a consequence
//         - Cache the angles (camX)
//         - Use Array.fill instead?
//         - Use Bitmap.LockBits as a potential optimization? Apparently it's faster than iterating over a Color[]
//         - Review math with https://www.youtube.com/watch?v=eOCQfxRQ2pY
//         - Look into profiling techniques. Need to know how fast things are
type Wolfer () as this =
    inherit Game()

    let graphics = new GraphicsDeviceManager(this)
    let mutable spriteBatch = Unchecked.defaultof<_>
    let mutable spriteFont = Unchecked.defaultof<_>
    let mutable hud = ""
    let mutable playerPos = Vector2(10.5f , 10.0f)
    let mutable walkSpeed = 5.0f
    let mutable rotSpeed = 2.5f
    let mutable playerDir = Vector2(-1.0f, 0.0f)
    let mapWidth = 16
    let mmSize = 150
    let mutable plane = Vector2(0.0f, 0.66f)
    let mutable texture = Unchecked.defaultof<Texture2D>
    let mutable colors = Unchecked.defaultof<Color array>
    let mutable screenWidth = 0
    let mutable screenHeight = 0
    let fps = new FrameCounter()

    do
        this.Content.RootDirectory <- "Content"
        this.IsMouseVisible <- true

    override this.Initialize() =
        base.Initialize()

//        graphics.IsFullScreen <- true;
//        graphics.GraphicsProfile <- GraphicsProfile.HiDef;
//        graphics.PreferredBackBufferWidth <- 1920
//        graphics.PreferredBackBufferHeight <- 1080
//        graphics.PreferredBackBufferWidth <- 1600
//        graphics.PreferredBackBufferHeight <- 968
        graphics.SynchronizeWithVerticalRetrace <- false
        this.IsFixedTimeStep <- false
//        graphics.PreferredBackBufferWidth <- 1024
//        graphics.PreferredBackBufferHeight <- 768
        graphics.ApplyChanges()

        screenWidth <- this.GraphicsDevice.Viewport.Width
        screenHeight <- this.GraphicsDevice.Viewport.Height

        texture <- new Texture2D(this.GraphicsDevice, screenWidth, screenHeight)
        colors <- Array.create (screenWidth * screenHeight) Color.White

    override this.LoadContent() =
        spriteBatch <- new SpriteBatch(this.GraphicsDevice)
        spriteFont <- this.Content.Load<SpriteFont>("DefaultFont")

    override this.Update (gameTime) =
        let dt = float32 gameTime.ElapsedGameTime.TotalSeconds
        let kbState = Keyboard.GetState()
        if (kbState.IsKeyDown(Keys.Escape))
            then this.Exit();

        let rotSpeed = rotSpeed * dt
        let rotate speed =
            playerDir <- Vector2(playerDir.X * cos speed - playerDir.Y * sin speed,
                                 playerDir.X * sin speed + playerDir.Y * cos speed)
            plane <- Vector2(plane.X * cos speed - plane.Y * sin speed,
                             plane.X * sin speed + plane.Y * cos speed)

        if kbState.IsKeyDown Keys.Q then
            rotate rotSpeed
        else if kbState.IsKeyDown Keys.E then
            rotate -rotSpeed

        if kbState.IsKeyDown Keys.W then
            playerPos <- playerPos + playerDir * walkSpeed * dt
        else if kbState.IsKeyDown Keys.S then
            playerPos <- playerPos - playerDir * walkSpeed * dt

        let strafeDir = Vector2(playerDir.Y, -playerDir.X)
        if kbState.IsKeyDown Keys.A then
            playerPos <- playerPos + -strafeDir * walkSpeed * dt
        else if kbState.IsKeyDown Keys.D then
            playerPos <- playerPos + strafeDir * walkSpeed * dt

        let screenWidthF = float32 screenWidth

        for x = 0 to screenWidth - 1 do
            let camX = 2.0f * float32 x / screenWidthF - 1.0f
            let mutable hit = false

            let rayDir = playerDir + plane * camX
            let mutable mapPosX = int playerPos.X
            let mutable mapPosY = int playerPos.Y
            let mutable stepX = 0
            let mutable stepY = 0
            let mutable sideDist = Vector2.Zero
            let mutable side = -1
            let deltaDist = Vector2(abs (1.0f / rayDir.X), abs (1.0f / rayDir.Y))

            if rayDir.X < 0.0f then
                stepX <- -1
                sideDist.X <- (playerPos.X - float32 mapPosX) * deltaDist.X
            else
                stepX <- 1
                sideDist.X <- (float32 mapPosX + 1.0f - playerPos.X) * deltaDist.X
            if rayDir.Y < 0.0f then
                stepY <- -1
                sideDist.Y <- (playerPos.Y - float32 mapPosY) * deltaDist.Y
            else
                stepY <- 1
                sideDist.Y <- (float32 mapPosY + 1.0f - playerPos.Y) * deltaDist.Y
            while not hit do
                if sideDist.X < sideDist.Y then
                    sideDist.X <- sideDist.X + deltaDist.X
                    mapPosX <- mapPosX + stepX
                    side <- 0
                else
                    sideDist.Y <- sideDist.Y + deltaDist.Y
                    mapPosY <- mapPosY + stepY
                    side <- 1
                if map.[mapPosY * mapWidth + mapPosX ] > 0 then hit <- true

            let wallDist =
                if side = 0 then
                    (float32 mapPosX - playerPos.X + (1.0f - float32 stepX) / 2.0f) / rayDir.X
                else
                    (float32 mapPosY - playerPos.Y + (1.0f - float32 stepY) / 2.0f) / rayDir.Y

            let h = float32 screenHeight
            let lineHeight = int <| (h / wallDist) * 2.0f
            let mutable drawStart = float32 -lineHeight / 2.0f + h / 2.0f |> int
            if drawStart < 0 then drawStart <- 0.0f |> int
            let mutable drawEnd = float32 lineHeight / 2.0f + h / 2.0f |> int
            if drawEnd >= int h then drawEnd <- h - 0.0f |> int

            for y = 0 to screenHeight - 1 do
                let inline setColor color = colors.[y * screenWidth + x] <- color
                if y <= drawStart then
                    setColor Color.SkyBlue
                else if y > drawStart && y <= drawEnd then
//                if y > drawStart && y <= drawEnd then
                    if side = 1
                        then setColor <| Color.DarkGray
                        else setColor <| Color.Gray
                else
                    setColor Color.DarkOliveGreen

        base.Update(gameTime)

    override this.Draw (gameTime) =
//        this.GraphicsDevice.Clear Color.CornflowerBlue
        fps.Update(float32 gameTime.ElapsedGameTime.TotalSeconds)
        hud <- string <| int fps.AverageFramesPerSecond

        texture.SetData colors
        spriteBatch.Begin()

        spriteBatch.Draw(texture, Vector2.Zero, Color.White)
        spriteBatch.DrawString(spriteFont, hud, Vector2(float32 screenWidth - 150.0f, 10.0f) , Color.Red)

        spriteBatch.End()

        base.Draw(gameTime)
