using GraphQL.Types;
using RulEng.States;

namespace RulEng.GraphModels
{
    public class OperandKeyType : ObjectGraphType<OperandKey>
    {
        public OperandKeyType()
        {
            Field(x => x.SourceValueIds);
            Field(x => x.EntType).Description("The type of this entity");
            Field(x => x.EntityId).Description("The id of this entity");
        }
    }
}
