using System;
using System.Collections.Immutable;

namespace RulEng.States
{
    public interface IRulePrescription
    {
        /// <summary>
        /// The Id of the RuleResult that will receive the result of the Unary, Comparison or Collection Rule
        /// </summary>
        Guid RuleResultId { get; set; }

        /// <summary>
        /// The minimum number of entities required to perform this Rule, 1 for Unary or Collection, 2 for Comparitor
        /// </summary>
        int MinEntitiesRequired { get; }

        /// <summary>
        /// The maximum number of entities that will be used by this Rule, 1 for Unary, 2 for Comparitor, null for Collection
        /// </summary>
        int? MaxEntitiesUsed { get; set; }

        /// <summary>
        /// The Ids of all of the Entities that will be used in calculating the result of the Unary, Comparison or Collection Rule
        /// </summary>
        ImmutableList<ITypeKey> EntityIds { get; set; }
    }
}
