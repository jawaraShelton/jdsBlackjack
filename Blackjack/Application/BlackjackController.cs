﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Blackjack.Application
{
    class BlackjackController : IController
    {
        private readonly BlackjackModel model;

        public BlackjackController(BlackjackModel model)
        {
            this.model = model;
        }

        public bool Execute(string command)
        {
            bool retval = true;
            string[] args = command.Split(' ');

            switch (args[0])
            {
                case "bet":
                    if (int.TryParse(args[1], out int bet))
                        model.Bet(bet);
                    break;
                case "hit":
                    model.Hit();
                    break;
                case "stand":
                    model.Stand();
                    break;
                case "double down":
                    model.DoubleDown();
                    break;
                case "split":
                    model.Split();
                    break;
                case "surrender":
                    model.Surrender();
                    break;
                case "quit":
                    Environment.Exit(0);
                    break;
                default:
                    //  >>>>>[  Invalid Command. Notify View command failed.
                    //          -----
                    Console.WriteLine("Command invalid. Retry.");

                    retval = false;
                    break;
            }

            return retval;
        }
    }
}