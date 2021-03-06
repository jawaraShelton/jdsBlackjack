﻿using System;
using System.Collections.Generic;
using System.Text;

using Blackjack.Blackjack;

namespace Blackjack.Application
{
    class BlackjackModel : Model
    {
        //  >>>>>[  Game logic developed using rules found at 
        //          https://www.pagat.com/banking/blackjack.html
        //
        //          jds | 2019.01.23
        //          -----

        private BlackjackPlayer Player;
        private BlackjackDealer Dealer;
        private IView View;

        private Dictionary<String, Boolean> Commands = new Dictionary<String, Boolean>();
        private List<String> FlavorText = new List<String>();
        private List<String> ResultText = new List<String>();

        public BlackjackModel(BlackjackDealer Dealer, BlackjackPlayer Player)
        {
            //  >>>>>[  For now keeping it simple: One player, one view. 
            //          - jds | 2019.01.30
            //          -----
            this.Dealer = Dealer;
            this.Player = Player;

            Dealer.NewHand();
            Player.NewHand();

            //  >>>>>[  Populate the command list. Initially, the only available
            //          command will be "Bet".
            //          -jds | 2019.01.31
            //          -----
            this.Commands.Add("bet", true);
            this.Commands.Add("hit", false);
            this.Commands.Add("stand", false);
            this.Commands.Add("double down", false);
            this.Commands.Add("restart", true);
            this.Commands.Add("surrender", false);
            this.Commands.Add("split", false);
            this.Commands.Add("quit", true);
        }

        public Boolean CommandAvailable(String command)
        {
            if (Commands.ContainsKey(command))
                return Commands[command];
            else
                return false;
        }

        public void LinkView(IView View)
        {
            this.View = View;
        }

        public String AvailableCommands()
        {
            StringBuilder outStr = new StringBuilder();

            foreach (KeyValuePair<String, Boolean> d in Commands)
                if (d.Value)
                    if(d.Key.Substring(0, 1).Equals("s"))              
                        outStr.Append("[" + d.Key.Substring(0,2) + "]" + d.Key.Substring(2) + " | ");
                    else
                        outStr.Append("[" + d.Key.Substring(0, 1) + "]" + d.Key.Substring(1) + " | ");

            return outStr.ToString().Substring(0, outStr.Length - 3).Trim();
        }

        public String GetPhase()
        {
            if (Commands["bet"])
                return "bet";
            else
                return "play";
        }

        public List<String> GetFlavorText()
        {
            return FlavorText;
        }

        public List<String> GetResultText()
        {
            return ResultText;
        }

        public String GetPlayerHand()
        {
            if(!Player.ShowHand().Equals("EMPTY"))
            {
                StringBuilder retval = new StringBuilder();

                foreach (BlackjackHand pHand in Player.playerHand)
                {
                    if (Player.playerHand.Count > 1 && pHand.ToString().Equals(Player.CurrentHand().ToString()))
                        retval.Append(">> ");

                    retval.Append(pHand.ToString() + " | ");
                }

                return retval.ToString().Substring(0, retval.Length - 3);
            }
            else
            {
                return Player.ShowHand();
            }
        }

        public String GetDealerHand()
        {
            return Dealer.ShowHand();
        }

        public decimal GetWager()
        {
            return Player.Bet;
        }

        public BlackjackPlayer GetPlayer()
        {
            return Player;
        }

        public decimal GetCashAvailable()
        {
            return Player.Cash;
        }

        public void Bet(decimal amount)
        {
            //  >>>>>[  Common chip values, colors, and slang in Las Vegas Casinos
            //
            //          Value       Color                           Nickname                        Notes
            //          -------     ---------------------------     --------------------------      ---------------------------
            //          $     1     White || Blue
            //          $     2     Odd Green Color                 "Limes", also "Mints"           "Mints" @ Caesar's Palace, "Limes" @ The Venetian.
            //          $     5     Red                             "Nickels", also "Redbirds"
            //          $    25     Green                           "Quarters"
            //          $   100     Black                           "Blackbirds"?
            //          $   500     Purple                          "Barneys"
            //          $ 1,000     Orange                          "Pumpkins", "Banana"?
            //          $ 5,000     White w/ Red and Blue Spots     "Flags"                         May be unique to The Bellagio
            //          $25,000     Apperance Unknown               "Cranberries"                   May be unique to The Bellagio
            //          -----
            if (amount > 0)
            {
                if (amount <= Player.Cash)
                {
                    Player.Bet = amount;
                    Player.Cash -= amount;

                    List<string> keyList = new List<string>(Commands.Keys);
                    List<string> outList = new List<string>() { "bet", "restart" };
                    foreach (string str in keyList)
                        Commands[str] = (!outList.Contains(str));

                    Deal();
                }
                else
                {
                    ResultText.Add("Bets must be whole dollar values less than or equal to your available cash.");
                    View.ModelChanged();
                }
            }
            else
            {
                ResultText.Add("Bets must be whole dollar values higher than $0.00");
                View.ModelChanged();
            }
        }

        private void ResetCommandAvailability()
        {
            List<string> keyList = new List<string>(Commands.Keys);
            List<string> outList = new List<string>() { "bet", "restart", "quit" };
            foreach (string str in keyList)
                Commands[str] = outList.Contains(str);
        }

        private void NoSurrender()
        {
            Player.NoSurrender();
            Commands["surrender"] = false;
        }

        public void Deal()
        {
            //  >>>>>[  Clear Player and Dealer's hand.
            //          -----
            Player.NewHand();
            Dealer.NewHand();

            //  >>>>>[  Shuffle the deck.
            //          -----
            Dealer.Shuffle();

            //  >>>>>[  Initial Deal
            //          -----
            for (int SubIndex = 0; SubIndex < 2; SubIndex++)
            {
                Player.AddToHand(Dealer.Deal());
                Dealer.AddToHand(Dealer.Deal());
            }

            FlavorText.Clear();
            ResultText.Clear();

            //  >>>>>[  Dealer checks for Blackjack. No blackjack?
            //          Play proceeds as normal...
            //          -----

            FlavorText.Add("+----");
            FlavorText.Add("The dealer takes a peek at her face-down card... ");
            if(Dealer.PlayerHand[0].IsBlackjack())
            {
                FlavorText.Add("Dealer has Blackjack!");
                DealerGo(true);
            }
            else
            {
                Commands["split"] = Player.CanSplit;

                View.ModelChanged();
            }
        }

        public void Restart()
        {
            FlavorText.Clear();
            ResultText.Clear();

            Player.Cash = 500;

            View.ModelChanged();
        }

        public void Hit()
        {
            //  >>>>>[  Signal: Scrape cards against table (in handheld games); 
            //          tap the table with finger or wave hand toward body 
            //          (in games dealt face up).
            //          -----

            if(Commands["hit"])
            {
                NoSurrender();

                Player.AddToHand(Dealer.Deal());

                FlavorText.Clear();
                ResultText.Clear();

                FlavorText.Add("The Dealer slides you a card.");

                if (Player.CurrentHand().Bust)
                {
                    ResultText.Add("Player's Hand: " + Player.CurrentHand().ToString() + " BUSTS!");

                    Player.AdvanceHand();
                    if (Player.CurrentHand().Bust)
                    {
                        View.ModelChanged(true);
                        DealerGo();
                    }
                    else
                    {
                        View.ModelChanged();
                    }
                }
                else
                {
                    View.ModelChanged();
                }
            }
            else
            {
                FlavorText.Add("Command not available.");
                View.ModelChanged();
            }
        }

        public void Stand()
        {
            // >>>>>[   Signal: Slide cards under chips (in handheld games); 
            //          wave hand horizontally (in games dealt face up).
            //          -----
            if (Commands["stand"])
            {
                FlavorText.Clear();
                ResultText.Clear();

                FlavorText.Add("Player stands.");
                Player.Stand();

                if (Player.CurrentHand().Standing)
                {
                    View.ModelChanged(true);

                    Dealer.PlayHand();
                    DealerGo();
                }
                else
                {
                    View.ModelChanged();
                }
            }
            else
            {
                FlavorText.Add("Command not available.");
                View.ModelChanged();
            }
        }

        public void DoubleDown()
        {
            // >>>>>[   Signal: Place additional chips beside the original bet 
            //          outside the betting box, and point with one finger.
            //          -----
            NoSurrender();

            FlavorText.Clear();
            ResultText.Clear();

            if (Commands["double down"])
            {
                if (Player.DoubleDown())
                {
                    FlavorText.Add("You place the additional chips beside your original bet--outside the betting box.");
                    FlavorText.Add("Player's bet is now $" + Player.Bet.ToString());

                    Player.AddToHand(Dealer.Deal());
                    Boolean LastHand = (Player.PlayerHand[Player.PlayerHand.Count - 1] == Player.CurrentHand());

                    if (Player.CurrentHand().Bust)
                    {
                        ResultText.Add("And the Player goes bust...");
                        ExitGracefully(LastHand);

                    }
                    else
                    {
                        Player.Stand();
                        ResultText.Add("Player stands.");
                        ExitGracefully(LastHand);

                    }
                }
                else
                {
                    FlavorText.Add("You do not have enough money for that.");
                    View.ModelChanged();
                }
            }
            else
            {
                FlavorText.Add("Command not available.");
                View.ModelChanged();
            }

            void ExitGracefully(Boolean LastHand)
            {
                if (LastHand)
                {
                    View.ModelChanged(true);
                    DealerGo();
                }
                else
                {
                    View.ModelChanged();
                }
            }
        }

        public void Split()
        {
            //  >>>>>[  Reset availability of Split command.
            //          -----
            Commands["split"] = false;

            //  >>>>>[  Signal: Place additional chips next to the original bet 
            //          outside the betting box; point with two fingers spread 
            //          into a V formation.
            //          -----
            FlavorText.Clear();

            if (Player.Split())
            {
                FlavorText.Add("You place an additional set of chips next to your original bet");
                FlavorText.Add("and point with your fingers spread to create a 'V'.");

                Player.AddToHand(Dealer.Deal());
                Player.AdvanceHand();
                Player.AddToHand(Dealer.Deal());
                Player.RetreatHand();
            }
            else
            {
                FlavorText.Add("You reach for more chips, then realize you don't have enough.");
            }

            View.ModelChanged();
        }

        public void Surrender()
        {
            //  >>>>>[  NO SIGNAL! The request to surrender is made verbally, 
            //          there being no standard hand signal.
            //
            //          When the player surrenders they give up the hand but 
            //          lose only half their bet as a result.
            //
            //          NOTE: Only available as first decision of hand.
            //          -----
            FlavorText.Clear();
            ResultText.Clear();

            if (Commands["surrender"])
            {
                if (Player.CanSurrender)
                {
                    Player.Surrender();

                    FlavorText.Add("You surrender the hand. Your bet is now " + Player.Bet.ToString("C"));
                    View.ModelChanged(true);

                    DealerGo();
                }
                else
                {
                    FlavorText.Add("That option is only available as the first decision of your hand.");
                }

                View.ModelChanged();
            }
            else
            {
                FlavorText.Add("Command not available.");
                View.ModelChanged();
            }
        }

        private void SetupNewHand()
        {
            Dealer.NewHand();
            Player.NewHand();

            Dealer.ResetReveal();

            Player.CanSurrender = true;
            //  Player.CurrentHand().Bust = false;
            //  Player.Standing = false;
            Player.Surrendered = false;
            Player.Bet = 0;

            FlavorText.Clear();
            ResultText.Clear();

            ResetCommandAvailability();

            View.ModelChanged();
        }

        private void DealerGo(Boolean PreserveFlavorText = false)
        {
            if(!PreserveFlavorText)
                FlavorText.Clear();

            ResultText.Clear();

            //  >>>>>[  Determine if the player has any hands
            //          in play as a pre-condition.
            //          -----
            Boolean PlayerIsBust = true;
            foreach(BlackjackHand pHand in Player.PlayerHand)
                PlayerIsBust = PlayerIsBust && pHand.Bust;

            //  >>>>>[  Play the Dealer's hand...
            //          -----
            if (!PlayerIsBust && !Dealer.PlayerHand[0].IsBlackjack())
            {
                FlavorText.Add("Dealer Plays...");
                Dealer.PlayHand();
            }

            FlavorText.Add("Dealer's Hand: " + Dealer.PlayerHand[0].ToString() + (Dealer.PlayerHand[0].Value() > 21 ? " BUSTS!" : ""));
            View.ModelChanged(true);
            FlavorText.Clear();

            //  >>>>>[  Score the hand, and distribute payouts.
            //          -----
            foreach (var pHand in Player.PlayerHand)
            {
                if (!pHand.Bust && !Player.Surrendered)
                {
                    if (Dealer.PlayerHand[0].Value() > 21)
                    {
                        Decimal winnings = pHand.IsBlackjack() ? pHand.Wager + (pHand.Wager * 1.5m) : pHand.Wager * 2;

                        ResultText.Add("Player's Hand: " + pHand.ToString() + " WINS " + winnings.ToString("C") + "!");

                        Player.Win(winnings);
                    }
                    else
                    {
                        if (pHand.Value() == Dealer.PlayerHand[0].Value())
                        {
                            if (Dealer.PlayerHand[0].IsBlackjack())
                            {
                                if (pHand.IsBlackjack())
                                    Push(pHand.ToString());
                                else
                                {
                                    ResultText.Add("Player's Hand: " + pHand.ToString() + " LOSES.");
                                    Player.LoseWager();
                                }
                            }
                            else
                            {
                                Push(pHand.ToString());
                            }
                        }

                        if (pHand.Value() < Dealer.PlayerHand[0].Value())
                        {
                            ResultText.Add("Player's Hand: " + pHand.ToString() + " LOSES.");
                            Player.LoseWager();
                        }

                        if (pHand.Value() > Dealer.PlayerHand[0].Value())
                        {
                            Decimal winnings = pHand.IsBlackjack() ? pHand.Wager + (pHand.Wager * 1.5m) : pHand.Wager * 2;
                            ResultText.Add("Player's Hand: " + pHand.ToString() + " WINS " + winnings.ToString("C") + "!");
                            Player.Win(winnings);
                        }
                    }
                }
                else
                {
                    ResultText.Add("Player's Hand: " + pHand.ToString() + (Player.Surrendered ? " was surrendered." : (pHand.Bust ? " BUSTS!" : " loses.")));
                    Player.LoseWager();
                }
            }

            ResultText.Add("----+");

            View.ModelChanged(true);
            SetupNewHand();

            void Push(String pHand)
            {
                //  >>>>>[  Dealer's hand should be displayed before reaching this point.
                //          Preserving the line justincase.
                //          -----
                //  ResultText.Add("Dealer's Hand: " + Dealer.PlayerHand[0].ToString());
                ResultText.Add("Player's Hand: " + pHand.ToString() + " is a push.");
                Player.Push();
            }
        }
    }
}