module Wolfer

open System.IO
open Microsoft.Xna.Framework
open Microsoft.Xna.Framework.Graphics
open Microsoft.Xna.Framework.Input

// TODO:
// 1.) Collision detection
// 2.) Billboarding
// 3.) Even further potential optimizations
//     - Either parallelize the whole raycasting routine, or the blitting loop
//     - Use Bitmap.LockBits as a potential optimization? Apparently it's faster than iterating over a Color[]
//     - Review math with https://www.youtube.com/watch?v=eOCQfxRQ2pY

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
    let sky = Color.SkyBlue
    let ground = Color.DarkOliveGreen
    let texWidth = 64
    let mutable screenWidth = 0
    let mutable screenHeight = 0
    let fps = Core.FrameCounter()
    let mutable textures = Array.zeroCreate 11
    let mutable groundTex = Unchecked.defaultof<Texture2D>
    let mutable heights = Array.create 1920 (0,0)
//    let mutable timeElapsed = 0.0f
//    let measure = new Core.Measure()

    static let createMap =
        let file = File.ReadAllText("map.txt").Replace("\n", "")
        Array.init file.Length (fun i -> match string file.[i] with " " -> 0uy | d -> System.Byte.Parse d)

    do
        this.Content.RootDirectory <- "Content"
        this.IsMouseVisible <- true

    override this.Initialize() =
        base.Initialize()

        graphics.IsFullScreen <- true;
//        graphics.GraphicsProfile <- GraphicsProfile.HiDef;
        graphics.PreferredBackBufferWidth <- 1920
        graphics.PreferredBackBufferHeight <- 1080
        graphics.SynchronizeWithVerticalRetrace <- false
        this.IsFixedTimeStep <- false
        graphics.ApplyChanges()

        screenWidth <- this.GraphicsDevice.Viewport.Width
        screenHeight <- this.GraphicsDevice.Viewport.Height
//        texture  <- new Texture2D(this.GraphicsDevice, screenHeight, screenWidth)
//        colors <- Array.create (screenWidth * screenHeight) Color.White

    override this.LoadContent() =
        spriteBatch <- new SpriteBatch(this.GraphicsDevice)
        spriteFont <- this.Content.Load<SpriteFont>("DefaultFont")
        groundTex <- new Texture2D(this.GraphicsDevice, 1, 1)
        groundTex.SetData([| Color.DarkOliveGreen |])
        let getPixels (t : Texture2D) =
            let length = texWidth * texWidth
            let mutable pixels : Color array = Array.zeroCreate length
//            let mutable target : Color array = Array.zeroCreate length
            t.GetData(pixels)
//            for i = 0 to length - 1 do
//                printfn "%i %i %i %i" length (i / texWidth) (i * texWidth) (i / texWidth + i * texWidth)
//                target.[i] <- pixels.[i / texWidth + (i % texWidth) * texWidth]
//                target.[i] <- pixels.[i / texWidth + (i % texWidth) * texWidth]
            pixels
//            target
        textures.[0]  <- this.Content.Load<Texture2D> "textures/wood"
        textures.[1]  <- this.Content.Load<Texture2D> "textures/greystone"
        textures.[2]  <- this.Content.Load<Texture2D> "textures/mossy"
        textures.[3]  <- this.Content.Load<Texture2D> "textures/colorstone"
        textures.[4]  <- this.Content.Load<Texture2D> "textures/redbrick"
        textures.[5]  <- this.Content.Load<Texture2D> "textures/purplestone"
        textures.[6]  <- this.Content.Load<Texture2D> "textures/bluestone"
        textures.[7]  <- this.Content.Load<Texture2D> "textures/eagle"
        textures.[8]  <- this.Content.Load<Texture2D> "textures/barrel"
        textures.[9]  <- this.Content.Load<Texture2D> "textures/pillar"
        textures.[10] <- this.Content.Load<Texture2D> "textures/greenlight"

    override this.Update (gameTime) =
//        measure.Reset()
        let dt = float32 gameTime.ElapsedGameTime.TotalSeconds
        let kbState = Keyboard.GetState()
        if (kbState.IsKeyDown(Keys.Escape))
            then this.Exit();

        let rotSpeed = rotSpeed * dt
        let rotate speed =
            playerDir <- Vector2(playerDir.X * cos speed - playerDir.Y * sin speed,
                                 playerDir.X * sin speed + playerDir.Y * cos speed)
            plane     <- Vector2(plane.X * cos speed - plane.Y * sin speed,
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

            heights.[x] <- (drawEnd - drawStart, texX)

//        timeElapsed <- timeElapsed + dt
//        if timeElapsed > 2.0f then
//            printfn "%f" <| measure.GetAverage()
//            measure.SortAndPrint()
//            timeElapsed <- 0.0f

        base.Update(gameTime)

    override this.Draw (gameTime) =
        this.GraphicsDevice.Clear Color.SkyBlue
        fps.Update(float32 gameTime.ElapsedGameTime.TotalSeconds)
        hud <- string <| int fps.AverageFramesPerSecond


        spriteBatch.Begin()

        let w = this.GraphicsDevice.Viewport.Width
        spriteBatch.Draw(groundTex, Rectangle(0, screenHeight / 2, screenWidth, screenHeight / 2), Color.White)

        for i = 0 to screenWidth - 1 do
            let h = fst heights.[i]
            let texX = snd heights.[i]
            let y = 1080 - h / 2
            spriteBatch.Draw(textures.[0], Rectangle(i, y, 1, h), System.Nullable(Rectangle(texX, 0, 2, 64)), Color.White)
        spriteBatch.DrawString(spriteFont, hud, Vector2(float32 w - 150.0f, 10.0f) , Color.Red)

        spriteBatch.End()

        base.Draw(gameTime)
