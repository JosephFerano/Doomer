module Wolfer

open System.IO
open Microsoft.Xna.Framework
open Microsoft.Xna.Framework.Graphics
open Microsoft.Xna.Framework.Input

// TODO:
// 1.) We have to look into billboarding for props, pick ups, and enemies
// 1.) We have to look into billboarding
// 5.) Texture blitting can be optimized in several ways;
//     - Rotate canvas 90 clockwise, then fill in by rows, rather than by column, then roate in draw
//     - Either parallelize the whole raycasting routine, or the blitting loop
//     - Have 2-3 color buffers and swap them, in case GPU is still processing and blocking as a consequence
//     - Cache the angles (camX)
//     - Use Array.fill instead?
//     - Use Bitmap.LockBits as a potential optimization? Apparently it's faster than iterating over a Color[]
//     - Review math with https://www.youtube.com/watch?v=eOCQfxRQ2pY
//     - Look into profiling techniques. Need to know how fast things are
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
    let mutable plane = Vector2(0.0f, 0.66f)
    let mutable texture = Unchecked.defaultof<Texture2D>
    let mutable colors = Unchecked.defaultof<Color array>
    let texWidth = 64
    let mutable screenWidth = 0
    let mutable screenHeight = 0
    let fps = new FrameCounter()
    let mutable textures = Array.zeroCreate 11

    let createMap =
        let file = File.ReadAllText("map.txt")

        let file = file.Replace("\n", "")
        Array.init file.Length (fun i ->
            match string file.[i] with
            | " " -> 0uy
            | d -> System.Byte.Parse d)

    do
        this.Content.RootDirectory <- "Content"
        this.IsMouseVisible <- true

    override this.Initialize() =
        base.Initialize()

//        graphics.IsFullScreen <- true;
//        graphics.GraphicsProfile <- GraphicsProfile.HiDef;
//        graphics.PreferredBackBufferWidth <- 1920
//        graphics.PreferredBackBufferHeight <- 1080
        graphics.SynchronizeWithVerticalRetrace <- false
        this.IsFixedTimeStep <- false
        graphics.PreferredBackBufferWidth <- 1024
        graphics.PreferredBackBufferHeight <- 576
        graphics.ApplyChanges()

        screenWidth <- this.GraphicsDevice.Viewport.Width
        screenHeight <- this.GraphicsDevice.Viewport.Height
//        screenWidth <- 512
//        screenHeight <- 288
        texture <- new Texture2D(this.GraphicsDevice, screenWidth, screenHeight)
        colors <- Array.create (screenWidth * screenHeight) Color.White

    override this.LoadContent() =
        spriteBatch <- new SpriteBatch(this.GraphicsDevice)
        spriteFont <- this.Content.Load<SpriteFont>("DefaultFont")
        let getPixels (t : Texture2D) =
            let mutable pixels : Color array = Array.zeroCreate (texWidth * texWidth)
            t.GetData(pixels)
            pixels
        textures.[0]  <- getPixels (this.Content.Load<Texture2D>("textures/wood"))
        textures.[1]  <- getPixels (this.Content.Load<Texture2D>("textures/greystone"))
        textures.[2]  <- getPixels (this.Content.Load<Texture2D>("textures/mossy"))
        textures.[3]  <- getPixels (this.Content.Load<Texture2D>("textures/colorstone"))
        textures.[4]  <- getPixels (this.Content.Load<Texture2D>("textures/redbrick"))
        textures.[5]  <- getPixels (this.Content.Load<Texture2D>("textures/purplestone"))
        textures.[6]  <- getPixels (this.Content.Load<Texture2D>("textures/bluestone"))
        textures.[7]  <- getPixels (this.Content.Load<Texture2D>("textures/eagle"))
        textures.[8]  <- getPixels (this.Content.Load<Texture2D>("textures/barrel"))
        textures.[9]  <- getPixels (this.Content.Load<Texture2D>("textures/pillar"))
        textures.[10] <- getPixels (this.Content.Load<Texture2D>("textures/greenlight"))

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
            let mutable wallIndex = 0
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
                let index = mapPosY * mapWidth + mapPosX
                if createMap.[index] > 0uy then
                    wallIndex <- createMap.[index] - 1uy |> int
                    hit <- true

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

            let wallX =
                let n = if side = 0
                            then playerPos.Y + wallDist * rayDir.Y
                            else playerPos.X + wallDist * rayDir.X
                n - floor n

            let texX =
                let n = int (wallX * float32 texWidth)
                if (side = 0 && rayDir.X > 0.0f) || (side = 1 && rayDir.Y < 0.0f)
                    then texWidth - n - 1
                    else n


            for y = 0 to screenHeight - 1 do
                let setColor color = colors.[y * screenWidth + x] <- color
                if y <= drawStart then
                    setColor Color.SkyBlue
                else if y > drawStart && y < drawEnd then
                    let d = y * 256 - int h * 128 + lineHeight * 128
                    let texY = d * texWidth / lineHeight / 256
                    let pixel = textures.[wallIndex].[texWidth * int texY + texX]
                    if side = 1
                        then setColor <| Color(pixel.R >>> 1 |> int, pixel.G >>> 1 |> int, pixel.B >>> 1 |> int)
                        else setColor <| pixel
                else
                    setColor Color.DarkOliveGreen

//            for y = 0 to screenHeight - 1 do
//                let inline setColor color = colors.[y * screenWidth + x] <- color
//                if y <= drawStart then
//                    setColor Color.SkyBlue
//                else if y > drawStart && y <= drawEnd then
//                    if side = 1
//                        then setColor <| Color.DarkGray
//                        else setColor <| Color.Gray
//                else
//                    setColor Color.DarkOliveGreen

        base.Update(gameTime)

    override this.Draw (gameTime) =
//        this.GraphicsDevice.Clear Color.SkyBlue
        fps.Update(float32 gameTime.ElapsedGameTime.TotalSeconds)
        hud <- string <| int fps.AverageFramesPerSecond

        texture.SetData colors
        spriteBatch.Begin()

        let w = this.GraphicsDevice.Viewport.Width
        let h = this.GraphicsDevice.Viewport.Height
        spriteBatch.Draw(texture, new Rectangle(0, 0, w, h), Color.White)

        spriteBatch.End()

        spriteBatch.Begin()
        spriteBatch.DrawString(spriteFont, hud, Vector2(float32 w - 150.0f, 10.0f) , Color.Red)
        spriteBatch.End()

        base.Draw(gameTime)
