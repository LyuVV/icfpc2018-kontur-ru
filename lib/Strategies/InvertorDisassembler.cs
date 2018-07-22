using System;
using System.Collections.Generic;
using System.Linq;

using lib.Commands;
using lib.Models;
using lib.Primitives;
using lib.Utils;

using MoreLinq;

namespace lib.Strategies
{
    public class InvertorDisassembler : IAmSolver
    {
        private readonly IAmSolver assembler;
        private readonly Matrix sourceMatrix;

        public InvertorDisassembler(IAmSolver assembler, Matrix sourceMatrix)
        {
            this.assembler = assembler;
            this.sourceMatrix = sourceMatrix;
        }

        public IEnumerable<ICommand> Solve()
        {
            var commands = new Queue<ICommand>(assembler.Solve());
            return ReverseCommands(commands, sourceMatrix);
        }

        public static IEnumerable<ICommand> ReverseCommands(Queue<ICommand> commands, Matrix sourceMatrix)
        {
            var ticks = new List<List<ICommand>>();
            var state = new DeluxeState(new Matrix(sourceMatrix.R), sourceMatrix);
            while (commands.Any())
            {
                state.StartTick();
                var reversedTick = new List<(int, ICommand)>();
                var primaryBidBySecondaryBotPosition = new Dictionary<Vec, int>();
                var mByPrimaryBotPosition = new Dictionary<Vec, int>();
                foreach (var bot in state.Bots)
                {
                    var bid = bot.Bid;
                    var command = commands.Dequeue();
                    if (command is SMove smove) reversedTick.Add((bid, new SMove(new LongLinearDifference(-smove.shift.Shift))));
                    if (command is Fill fill) reversedTick.Add((bid, new Voidd(fill.Shift)));
                    if (command is Wait wait) reversedTick.Add((bid, wait));
                    if (command is LMove move) reversedTick.Add((bid, new LMove(new ShortLinearDifference(-move.secondShift.Shift), new ShortLinearDifference(-move.firstShift.Shift))));
                    if (command is Fission fission)
                    {
                        reversedTick.Add((bid, new FusionP(fission.shift)));
                        var newBotId = bot.Seeds[0];
                        reversedTick.Add((newBotId, new FusionS(new NearDifference(-fission.shift.Shift))));
                    }
                    if (command is FusionP fussionP)
                    {
                        var botPos = bot.Position;
                        var secondaryBotPosition = botPos + fussionP.shift.Shift;
                        if (mByPrimaryBotPosition.ContainsKey(botPos))
                        {
                            reversedTick.Add((bid, new Fission(fussionP.shift, mByPrimaryBotPosition[botPos])));
                        }
                        else
                        {
                            primaryBidBySecondaryBotPosition.Add(secondaryBotPosition, bid);
                        }
                    }
                    if (command is FusionS fussionS)
                    {
                        var botPos = bot.Position;
                        var primaryBotPosition = botPos + fussionS.shift.Shift;
                        if (primaryBidBySecondaryBotPosition.ContainsKey(botPos))
                        {
                            var primaryBid = primaryBidBySecondaryBotPosition[botPos];
                            reversedTick.Add((primaryBid, new Fission(new NearDifference(-fussionS.shift.Shift), bot.Seeds.Count)));
                        }
                        else
                            mByPrimaryBotPosition.Add(primaryBotPosition, bot.Seeds.Count);
                    }
                    state.SetBotCommand(bot, command);
                }
                if (reversedTick.Any())
                {
                    ticks.Add(reversedTick.OrderBy(t => t.Item1).Select(t => t.Item2).ToList());
                }
                state.EndTick();
            }
            ticks.Reverse();
            return ticks.SelectMany(cs => cs).Concat(new[] {new Halt()});
        }
    }
}