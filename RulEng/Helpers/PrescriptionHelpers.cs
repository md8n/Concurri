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

        /// <summary>
        /// Create an AddUpdate Prescription for the supplied Operation
        /// </summary>
        /// <param name="operationMx"></param>
        /// <returns>The Prescription containing the Operation to be performed</returns>
        public static OperationMxProcessing AddUpdate(this Operation operationMx)
        {
            return new AddUpdate<Operation>
            {
                Entity = operationMx
            };
        }

        /// <summary>
        /// Create a Delete Prescription for the supplied Operation
        /// </summary>
        /// <param name="operationDx"></param>
        /// <returns>The Prescription containing the Operation to be performed</returns>
        public static OperationDxProcessing Delete(this Operation operationDx)
        {
            return new DeleteEnt<Operation>
            {
                Entity = operationDx
            };
        }
    }
}
