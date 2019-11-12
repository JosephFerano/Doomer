open Doomer
open Wolfer

module Program =
    [<EntryPoint>]
    let main _ =
//        use game = new Doomer()
        use game = new Wolfer()
        game.Run()
        0 // return an integer exit code
