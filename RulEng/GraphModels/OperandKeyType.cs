using GraphQL.Types;
using RulEng.States;

namespace RulEng.GraphModels
{
    public class OperandKeyType : ObjectGraphType<OperandKey>
    {
        public OperandKeyType()
        {
            Field(x => x.SourceEntityIds);
            Field(x => x.SourceEntType).Description("The type of the source entities");
            Field(x => x.EntType).Description("The type of this entity");
            Field(x => x.EntTags).Description("The tags associated with this entity");
            Field(x => x.EntityId).Description("The id of this entity");
        }
    }
}
