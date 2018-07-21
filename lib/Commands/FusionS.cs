using JetBrains.Annotations;

using lib.Models;
using lib.Primitives;
using lib.Utils;

namespace lib.Commands
{
    public class FusionS : BaseCommand
    {
        private readonly NearDifference shift;

        public FusionS(NearDifference shift)
        {
            this.shift = shift;
        }

        public override string ToString()
        {
            return $"FusionS({shift})";
        }

        [NotNull]
        public override byte[] Encode()
        {
            return new[] {(byte)((shift.GetParameter() << 3) | 0b110)};
        }

        public override bool AllPositionsAreValid([NotNull] IMatrix matrix, Bot bot)
        {
            var pos = bot.Position + shift;
            if (!matrix.IsInside(bot.Position))
                return false;
            if (!matrix.IsInside(pos))
                return false;
            return true;
        }

        protected override void DoApply(MutableState mutableState, Bot bot)
        {
            // The whole work is done in FusionP
        }

        public override void Apply(DeluxeState state, Bot bot)
        {
            // The whole work is done in FusionP
        }

        public override Vec[] GetVolatileCells(Bot bot)
        {
            // Both volatile cells are in FusionP
            return new Vec[0];
        }
    }
}