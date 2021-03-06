﻿using System;
using System.Collections.Generic;
using Blackjack.Blackjack;

namespace Blackjack
{
    abstract class Dealer: IPlayer
    {
        #region IPlayer Implementation
        //  >>>>>[  Implement interface IPlayer
        //          - jds | 2019.01.25
        //          -----
        protected List<BlackjackHand> playerHand;
        public List<BlackjackHand> PlayerHand
        {
            get => playerHand;
            set => playerHand = value;
        }

        public String playerName;
        string IPlayer.PlayerName
        {
            get => playerName;
            set => playerName = value;
        }

        public void NewHand()
        {
            if (PlayerHand != null)
                playerHand.Clear();
        }

        public void AddToHand(string Card)
        {
            if (playerHand.Count == 0)
                playerHand.Add(new BlackjackHand());

            playerHand[0].Add(Card);
        }

        public virtual string ShowHand()
        {
            return playerHand[0].Show();
        }

        #endregion


        protected Shoe shoe;
        protected List<IPlayer> players;
        protected Int16 ptrCur = 0;

        public Dealer()
        {
            playerHand = new List<BlackjackHand>
            {
                new BlackjackHand()
            };

            players = new List<IPlayer>();
            Shuffle();
        }

        public abstract void Shuffle(int numberOfDecks = 1);
        public abstract String Deal();

        public abstract String PlayHand();

        public void AddPlayer(IPlayer player)
        {
            players.Add(player);
        }
    }
}
