using Redux;
using RulEng.Prescriptions;
using RulEng.States;
using System.Collections.Immutable;
using System.Linq;

namespace RulEng.Helpers
{
    public static class PrescriptionHelpers
    {
        public static IRuleProcessing Exists(this Rule existsRule)
        {
            return new ProcessExistsRule
            {
                Entities = existsRule.ReferenceValues
            };
        }

        public static IRuleProcessing HasMeaningfulValue(this Rule hasMeangingfulValueRule)
        {
            return new ProcessHasMeaningfulValueRule
            {
                Entities = ImmutableList.CreateRange(hasMeangingfulValueRule.ReferenceValues)
            };
        }

        public static ICrud Create(this Operation createOperation)
        {
            return new Create<Operation>
            {
                Entity = createOperation
            };
        }
    }
}
