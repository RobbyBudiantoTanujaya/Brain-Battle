using System;

namespace BrainBattle.Core.Models
{
    [Serializable]
    public enum CellState : byte
    {
        Empty = 0,
        Dot   = 1,
        Crown = 2
    }
}
