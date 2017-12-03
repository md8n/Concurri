using System;

namespace RulEng.States
{
    public interface IRuleCompare
    {
        Guid ValueAId { get; set; }
        Guid ValueBId { get; set; }
    }
}
