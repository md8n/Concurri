using System;
using System.Collections.Immutable;
using RulEng.States;
using System.Collections.Generic;

namespace RulEng.Helpers
{
    public static class RuleHelpers
    {
        public static Rule ExistsRule(this ITypeKey value)
        {
            var rule = new Rule
            {
                RuleId = Guid.NewGuid(),
                RuleName = "Test for non-existence " + ((TypeKey)value).ToString(),
                RuleType = RuleType.Exists,
                NegateResult = true,
                ReferenceValues = ImmutableArray.Create(value)
            };

            return rule;
        }

        public static Rule ExistsRule(this IEnumerable<ITypeKey> values)
        {
            var rule = new Rule
            {
                RuleId = Guid.NewGuid(),
                RuleName = "Test for non-existence of entities",
                RuleType = RuleType.Exists,
                NegateResult = true,
                ReferenceValues = ImmutableArray.CreateRange(values)
            };

            return rule;
        }
    }
}
