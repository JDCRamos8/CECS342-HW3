/// Card representations.
// An "enum"-type union for card suit.
type CardSuit = 
    | Spades 
    | Clubs
    | Diamonds
    | Hearts

// Kinds: 1 = Ace, 2 = Two, ..., 11 = Jack, 12 = Queen, 13 = King.
type Card = {suit : CardSuit; kind : int}


/// Game state records.
// One hand being played by the player: its cards, and a flag for whether it was doubled-down.
type PlayerHand = {
    cards: Card list; 
    doubled: bool
}

// All the hands being played by the player: the hands that are still being played (in the order the player must play them),
// and the hands that have been finished (stand or bust).
type PlayerState = {
    activeHands: PlayerHand list; 
    finishedHands: PlayerHand list
}

// The state of a single game of blackjack. Tracks the current deck, the player's hands, and the dealer's hand.
type GameState = { 
    deck : Card list; 
    player : PlayerState; 
    dealer: Card list
}

// A log of results from many games of blackjack.
type GameLog = {playerWins : int; dealerWins : int; draws : int}

/// Miscellaneous enums.
// Identifies whether the player or dealer is making some action.
type HandOwner = 
    | Player 
    | Dealer

// The different actions a player can take.
type PlayerAction = 
    | Hit
    | Stand
    | DoubleDown
    | Split

// The result of one hand that was played.
type HandResult = 
    | Win
    | Lose
    | Draw


// This global value can be used as a source of random integers by writing
// "rand.Next(i)", where i is the upper bound (exclusive) of the random range.
let rand = new System.Random()


// UTILITY METHODS

// Returns a string describing a card.
let cardToString card =
    // TODO: replace the following line with logic that converts the card's kind to a string.
    // Reminder: a 1 means "Ace", 11 means "Jack", 12 means "Queen", 13 means "King".
    // A "match" statement will be necessary. (The next function below is a hint.)
    let kind = 
       match card.kind with
       |1 -> "Ace"
       |2 -> "Two"
       |3 -> "Three"
       |4 -> "Four"
       |5 -> "Five"
       |6 -> "Six"
       |7 -> "Seven"
       |8 -> "Eight"
       |9 -> "Nine"
       |10 -> "Ten"
       |11 -> "Jack"
       |12 -> "Queen"
       |13 -> "King"
       |_ -> ""

    // "%A" can print any kind of object, and automatically converts a union (like CardSuit)
    // into a simple string.
    sprintf "%s of %A" kind card.suit


// Returns a string describing the cards in a hand.    
let handToString hand =
    // TODO: replace the following line with statement(s) to build a string describing the given hand.
    // The string consists of the results of cardToString when called on each Card in the hand (a Card list),
    // separated by commas. You need to build this string yourself; the built-in "toString" methods for lists
    // insert semicolons and square brackets that I do not want.
    hand
    |> List.map (cardToString) |> String.concat ", "

    // Hint: transform each card in the hand to its cardToString representation. Then read the documentation
    // on String.concat.


    
// Returns the "value" of a card in a poker hand, where all three "face" cards are worth 10
// and an Ace has a value of 11.
let cardValue card =
    match card.kind with
    | 1 -> 11
    | 11 | 12 | 13 -> 10  // This matches 11, 12, or 13.
    | n -> n
    
    // Reminder: the result of the match will be returned


// Calculates the total point value of the given hand (Card list). 
// Find the sum of the card values of each card in the hand. If that sum
// exceeds 21, and the hand has aces, then some of those aces turn from 
// a value of 11 to a value of 1, and a new total is computed.
// TODO: fill in the marked parts of this function.
let handTotal hand =
    // TODO: modify the next line to calculate the sum of the card values of each
    // card in the list. Hint: List.map and List.sum. (Or, if you're slick, List.sumBy)
    let sum = List.sumBy cardValue hand

    // TODO: modify the next line to count the number of aces in the hand.
    // Hint: List.filter and List.length.
    let numAces = List.length(List.filter (fun c -> c.kind = 1) hand)

    // Adjust the sum if it exceeds 21 and there are aces.
    if sum <= 21 then
        // No adjustment necessary.
        sum
    else 
        // Find the max number of aces to use as 1 point instead of 11.
        let maxAces = (float sum - 21.0) / 10.0 |> ceil |> int
        // Remove 10 points per ace, depending on how many are needed.
        sum - (10 * (min numAces maxAces))


// FUNCTIONS THAT CREATE OR UPDATE GAME STATES

// Creates a new, unshuffled deck of 52 cards.
// A function with no parameters is indicated by () in the parameter list. It is also invoked
// with () as the argument.
let makeDeck () =
    // Make a deck by calling this anonymous function 52 times, each time incrementing
    // the parameter 'i' by 1.
    // The Suit of a card is found by dividing i by 13, so the first 13 cards are Spades.
    // The Kind of a card is the modulo of (i+1) and 13. 
    List.init 52 (fun i -> let s = match i / 13 with
                                   | 0 -> Spades
                                   | 1 -> Clubs
                                   | 2 -> Diamonds
                                   | 3 -> Hearts
                           {suit = s; kind = i % 13 + 1})


// Shuffles a list by converting it to an array, doing an in-place Fisher-Yates 
// shuffle, then converting back to a list.
// Don't worry about this.
let shuffleDeck deck =
    let arr = List.toArray deck

    let swap (a: _[]) x y =
        let tmp = a.[x]
        a.[x] <- a.[y]
        a.[y] <- tmp
    
    Array.iteri (fun i _ -> swap arr i (rand.Next(i, Array.length arr))) arr
    Array.toList arr


// Creates a new game state using the given deck, dealing 2 cards to the player and dealer.
let newGame (deck : Card list) =
    // Construct the starting hands for player and dealer.
    let playerCards = [deck.Head ; List.item 2 deck] // First and third cards.
    let dealerCards = [deck.Tail.Head ; List.item 3 deck] // Second and fourth.

    // Return a fresh game state.
    {deck = List.skip 4 deck;
    // the initial player has only one active hand.
     player = {activeHands = [{cards = playerCards; doubled = false}]; finishedHands = []}
     dealer = dealerCards}


// Given a current game state and an indication of which player is "hitting", deal one
// card from the deck and add it to the given person's hand. Return the new game state.
let hit handOwner gameState = 
    let topCard = List.head gameState.deck
    let newDeck = List.tail gameState.deck
    
    // Updating the dealer's hand is easy.
    if handOwner = Dealer then
        let newDealerHand = topCard :: gameState.dealer
        // Return a new game state with the updated deck and dealer hand.
        {gameState with deck = newDeck;
                        dealer = newDealerHand}
    else
        // TODO: updating the player is trickier. We are always working with the player's first
        // active hand. Create a new first hand by adding the top card to that hand's card list.
        // Then update the player's active hands so that the new first hand is head of the list; and the
        //     other (unchanged) active hands follow it.
        // Then construct the new game state with the updated deck and updated player.

        // TODO: this is just so the code compiles; fix it.
        let newFirstHand = { cards = topCard :: gameState.player.activeHands.Head.cards
                             doubled = gameState.player.activeHands.Head.doubled }
        let updatedPlayer = { activeHands = newFirstHand :: gameState.player.activeHands.Tail 
                              finishedHands = gameState.player.finishedHands }

        {gameState with 
                   deck = newDeck
                   player = updatedPlayer }

        
// Take the dealer's turn by repeatedly taking a single action, hit or stay, until 
// the dealer busts or stays.
let rec dealerTurn gameState =
    let dealer = gameState.dealer
    let score = handTotal dealer

    printfn "Dealer's hand: %s; %d points" (handToString dealer) score
    
    // Dealer rules: must hit if score < 17.
    if score > 21 then
        printfn "Dealer busts!"
        // The game state is unchanged because we did not hit. 
        // The dealer does not get to take another action.
        gameState
    elif score < 17 then
        printfn "Dealer hits"
        // The game state is changed; the result of "hit" is used to build the new state.
        // The dealer gets to take another action using the new state.
        gameState
        |> hit Dealer
        |> dealerTurn
    else
        // The game state is unchanged because we did not hit. 
        // The dealer does not get to take another action.
        printfn "Dealer must stay"
        gameState


// Take the player's turn by repeatedly taking a single action until they bust or stay.
let rec playerTurn (playerStrategy : GameState->PlayerAction) (gameState : GameState) =
    // TODO: code this method using dealerTurn as a guide. Follow the same standard
    // of printing output. This function must return the new game state after the player's
    // turn has finished, like dealerTurn.

    // Unlike the dealer, the player gets to make choices about whether they will hit or stay.
    // The "elif score < 17" code from dealerTurn is inappropriate; in its place, we will
    // allow a "strategy" to decide whether to hit. A "strategy" is a function that accepts
    // the current game state and returns true if the player should hit, and false otherwise.
    // playerTurn must call that function (the parameter playerStrategy) to decide whether
    // to hit or stay.
    let playerState = gameState.player

    if playerState.activeHands.IsEmpty then
        // A player with no active hands cannot take an action.
        gameState
    else
        // The next line is just so the code compiles. Remove it when you code the function.
        // TODO: print the player's first active hand. Call the strategy to get a PlayerAction.
        // Create a new game state based on that action. Recurse if the player can take another action 
        // after their chosen one, or return the game state if they cannot.
        let player = playerState.activeHands.Head.cards
        let score = handTotal player
        
        printfn "Player's hand: %s; %d points" (handToString player) score

        if score > 21 then
            printfn "Player busts!"
            gameState
        else
            let playerAction = playerStrategy gameState

            match playerAction with
            |Hit -> printfn "Player hits"
                    gameState |> hit Player |> playerTurn playerStrategy
            |DoubleDown -> 
                printfn "Player double downs"
                let updatedActiveHand = {playerState.activeHands.Head with
                                                                      cards = playerState.activeHands.Head.cards
                                                                      doubled = true }
                let updatedPlayerHands = {playerState with
                                                      activeHands = updatedActiveHand :: playerState.activeHands.Tail
                                                      finishedHands = playerState.finishedHands }
            
                playerTurn playerStrategy {gameState with 
                                                     player = updatedPlayerHands }
            |Split -> 
                printfn "Player splits"
                let playerHand1 =
                    {playerState.activeHands.Head with
                                                  cards = playerState.activeHands.Head.cards.Head :: [] }

                let playerHand2 =
                    {playerState.activeHands.Head with
                                                  cards = playerState.activeHands.Head.cards.Tail.Head :: [] }

                let updatedPlayerHands = {playerState with
                                                      activeHands = playerHand1 :: playerHand2 ::  playerState.activeHands.Tail
                                                      finishedHands = playerState.finishedHands }

                playerTurn playerStrategy {gameState with 
                                                     player = updatedPlayerHands }

            |_ -> printfn "Player must stay"
                  gameState


// Moves the player's first active hand to their inactive hands and returns a new gamestate.
let moveActiveHand gameState = 
    let newActiveHand = gameState.player.activeHands.Tail 
    let updatedPlayer = { activeHands = newActiveHand
                          finishedHands = gameState.player.activeHands.Head :: gameState.player.finishedHands }

    {gameState with 
               deck = gameState.deck
               player = updatedPlayer
               dealer = gameState.dealer }


// Classifies a player's hand as a Win,Lose,or Draw compared to the dealer's hand              
let playerOutcome playerHand dealerHand = 
    let playerTotal = handTotal playerHand
    let dealerTotal = handTotal dealerHand

    if playerTotal <= 21 && (dealerTotal < playerTotal || dealerTotal > 21) then
        Win
    //else if dealerTotal <= 21 || playerTotal > 21 then
    else if dealerTotal <= 21 && (playerTotal < dealerTotal || playerTotal > 21) then
        Lose
    else
        Draw


// Plays one game with the given player strategy. Returns a GameLog recording the winner of the game.
let oneGame playerStrategy gameState =
    // TODO: print the first card in the dealer's hand to the screen, because the Player can see
    // one card from the dealer's hand in order to make their decisions.
    printfn "Dealer is showing: %A" (cardToString gameState.dealer.Head)
    let score1 = handTotal gameState.dealer
    let score2 = handTotal gameState.player.activeHands.Head.cards

    let wins = 0
    let losses = 0
    let draws = 0
    
    if score1 = 21 && score2 < 21 then
        {playerWins = wins; dealerWins = losses + 1; draws = draws}

    else if score1 = 21 && score1 = score2 then
        {playerWins = wins; dealerWins = losses; draws = draws + 1}

    else
        printfn "Player's turn"
        // TODO: play the game! First the player gets their turn. The dealer then takes their turn,
        // using the state of the game after the player's turn finished.
        let gameState2 = playerTurn playerStrategy gameState

        printfn "\nDealer's turn"
        let gameState3 = dealerTurn gameState2

        // TODO: determine the winner(s)! For each of the player's hands, determine if that hand is a 
        // win, loss, or draw. Accumulate (!!) the sum total of wins, losses, and draws, accounting for doubled-down
        // hands, which gets 2 wins, 2 losses, or 1 draw
        let rec oneGame' gameState wins losses draws =
            if not gameState.player.activeHands.IsEmpty then
                let outcome = playerOutcome (gameState.player.activeHands.Head.cards) gameState.dealer
                let increment =
                    if gameState.player.activeHands.Head.doubled = false then
                        1
                    else
                        2
                
                match outcome with
                | Win -> oneGame' (playerTurn playerStrategy (moveActiveHand gameState)) (wins + increment) losses draws
                | Lose -> oneGame' (playerTurn playerStrategy (moveActiveHand gameState)) wins (losses + increment) draws
                | Draw -> oneGame' (playerTurn playerStrategy (moveActiveHand gameState)) wins losses (draws + 1)
            else
                wins, losses, draws

        let result = oneGame' gameState3 0 0 0
        let wins, losses, draws = result

        // The player wins a hand if they did not bust (score <= 21) AND EITHER:
        // - the dealer busts; or
        // - player's score > dealer's score
        // If neither side busts and they have the same score, the result is a draw.

        // TODO: this is a "blank" GameLog. Return something more appropriate for each of the outcomes
        // described above.
        {playerWins = wins; dealerWins = losses; draws = draws}


// Plays n games using the given playerStrategy, and returns the combined game log.
let manyGames n playerStrategy =
    // TODO: run oneGame with the playerStrategy n times, and accumulate the result. 
    // If you're slick, you won't do any recursion yourself. Instead read about List.init, 
    // and then consider List.reduce.
    let gameLogs = List.init n (fun g -> makeDeck() |> shuffleDeck |> newGame |> oneGame playerStrategy)

    let pWins = List.reduce (+) (List.map (fun g -> g.playerWins) gameLogs)
    let dWins = List.reduce (+) (List.map (fun g -> g.dealerWins) gameLogs)
    let d = List.reduce (+) (List.map (fun g -> g.draws) gameLogs)

    // TODO: this is a "blank" GameLog. Return something more appropriate.
    {playerWins = pWins; dealerWins = dWins; draws = d}
           
        
// PLAYER STRATEGIES
// Returns a list of legal player actions given their current hand.
let legalPlayerActions playerHand =
    let legalActions = [Hit; Stand; DoubleDown; Split]
    // One boolean entry for each action; True if the corresponding action can be taken at this time.
    let requirements = [
        handTotal playerHand < 21; 
        true; 
        playerHand.Length = 2;
        playerHand.Length = 2 && cardValue playerHand.Head = cardValue playerHand.Tail.Head
    ]

    List.zip legalActions requirements // zip the actions with the boolean results of whether they're legal
    |> List.filter (fun (_, req) -> req) // if req is true, the action can be taken
    |> List.map (fun (act, _) -> act) // return the actions whose req was true


// Get a nice printable string to describe an action.
let actionToString = function
    | Hit -> "(H)it"
    | Stand -> "(S)tand"
    | DoubleDown -> "(D)ouble down"
    | Split -> "S(p)lit"

// This strategy shows a list of actions to the user and then reads their choice from the keyboard.
let rec interactivePlayerStrategy gameState =
    let playerHand = gameState.player.activeHands.Head
    let legalActions = legalPlayerActions playerHand.cards

    legalActions
    |> List.map actionToString
    |> String.concat ", "
    |> printfn "What do you want to do? %s" 

    let answer = System.Console.ReadLine()
    // Return true if they entered "y", false otherwise.
    match answer.ToLower() with
    | "h" when List.contains Hit legalActions -> Hit
    | "s" -> Stand
    | "d" when List.contains DoubleDown legalActions -> DoubleDown
    | "p" when List.contains Split legalActions -> Split
    | _ -> printfn "Please choose one of the available options, dummy."
           interactivePlayerStrategy gameState


// This strategy makes the player always stand.
let inactivePlayerStrategy gameState = 
    let playerHand = gameState.player.activeHands.Head
    let legalActions = legalPlayerActions playerHand.cards

    if List.contains Stand legalActions then 
        Stand
    else
        Stand
   
   
// This strategy makes the player always hit.
let greedyPlayerStrategy gameState = 
    let playerHand = gameState.player.activeHands.Head
    let legalActions = legalPlayerActions playerHand.cards
    
    if List.contains Hit legalActions then Hit else Stand
    
 
// This strategy makes the player always hit.
let coinFlipPlayerStrategy gameState = 
    let playerHand = gameState.player.activeHands.Head
    let legalActions = legalPlayerActions playerHand.cards

    if List.contains Hit legalActions && List.contains Stand legalActions then 
        let coinFace = rand.Next(2)

        if coinFace = 0 then
            Hit
        else 
            Stand
    else
        Stand


// This strategy makes the player do specific PlayerActions on basic conditions.
let basicPlayerStrategy gameState = 
    let playerHand = gameState.player.activeHands.Head
    let playerScore = handTotal playerHand.cards
    let dealerFirstCard = cardValue gameState.dealer.Head

    // DoubleDown
    if playerHand.doubled = false && (playerScore = 11 || playerScore = 10 || playerScore = 9) then
        if (playerScore = 10 && (dealerFirstCard = 10 || dealerFirstCard = 11) && not (cardValue playerHand.cards.Head = 5 && cardValue playerHand.cards.Tail.Head = 5)) then          
            Hit
        else if (playerScore = 9 && (dealerFirstCard = 2 || dealerFirstCard >= 7)) then 
            Hit
        else if cardValue playerHand.cards.Head = 5 && cardValue playerHand.cards.Tail.Head = 5 then
            DoubleDown
        else
            DoubleDown

    // Split
    elif playerHand.cards.Length = 2 && not(cardValue playerHand.cards.Head = 5 && cardValue playerHand.cards.Tail.Head = 5) && playerHand.cards.Head.kind = playerHand.cards.Tail.Head.kind then
        if playerScore = 20 then
            Stand
        else 
            Split

    // Otherwise...
    else
        if dealerFirstCard >= 2 && dealerFirstCard <= 6 then
            if playerScore >= 12 then
                Stand
            else
                Hit

        else if dealerFirstCard >= 7 && dealerFirstCard <= 10 then
            if playerScore <= 16 then
                Hit
            else
                Stand

        else if (dealerFirstCard = 11) then
            if (playerScore <= 16 && (cardValue playerHand.cards.Head = 11 || handTotal playerHand.cards.Tail = 11)) then // At least one Ace
                Hit
            else if playerScore <= 11 then
                Hit
            else
                Stand
        else 
            Stand


[<EntryPoint>]
let main argv =
    // TODO: call manyGames to run 1000 games with a particular strategy.
    manyGames 1000 basicPlayerStrategy
    |> printfn "%A"

    0 // return an integer exit code
