using RulEng.Prescriptions;
using RulEng.States;

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
                Entities = hasMeangingfulValueRule.ReferenceValues
            };
        }

        public static IRuleRuleResultProcessing And(this Rule andRule)
        {
            return new ProcessAndRule
            {
                Entities = andRule.ReferenceValues
            };
        }

        public static IRuleRuleResultProcessing Or(this Rule orRule)
        {
            return new ProcessOrRule
            {
                Entities = orRule.ReferenceValues
            };
        }

        public static IRuleRuleResultProcessing Xor(this Rule xorRule)
        {
            return new ProcessXorRule
            {
                Entities = xorRule.ReferenceValues
            };
        }

        public static IOpReqProcessing AddUpdate(this Operation OperationMx)
        {
            return new AddUpdate<Operation>
            {
                Entity = OperationMx
            };
        }
    }
}
