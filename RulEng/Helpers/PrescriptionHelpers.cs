using Redux;
using RulEng.Prescriptions;
using RulEng.States;

namespace RulEng.Helpers
{
    public static class PrescriptionHelpers
    {
        public static ICrud Exists(this Rule existsRule)
        {
            return new Create<Rule>
            {
                Entity = existsRule
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
