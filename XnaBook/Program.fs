namespace Playground
open Microsoft.VisualBasic

module Program =

    open System
    open Microsoft.Xna.Framework

    [<EntryPoint>]
    let main argv =
//        use game = new Game2D()
        use game = new Game3D()
//        use game = new Spaceship()
        game.Run()
        0 // return an integer exit code
