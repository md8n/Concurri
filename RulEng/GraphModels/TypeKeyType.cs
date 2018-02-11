using GraphQL.Types;
using RulEng.States;

namespace RulEng.GraphModels
{
    public class TypeKeyType : ObjectGraphType<IEntity>
    {
        public TypeKeyType()
        {
            Field(x => x.EntType).Description("The type of this entity");
            Field(x => x.EntityId).Description("The id of this entity");
        }
    }
}
