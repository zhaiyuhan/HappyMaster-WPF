using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HappyMaster.Foundation
{
    partial class HandleCommand
    {
        enum Commands
        {
            ERRO_COMMAND,
            PLAY_NO_URL,
            PLAY_WITH_URL
        };
        Commands _handle(string _input)
        {
            Commands _thiscommand = Commands.ERRO_COMMAND;
            string removeblank = _input.Trim().ToLower();
            string[] split = removeblank.Split(new Char[] { '(' });
            return _thiscommand;
        }
    }
}
