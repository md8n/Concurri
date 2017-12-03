using GraphQL.Types;
using RulEng.States;

namespace RulEng.GraphModels
{
    public class TypeKeyType : ObjectGraphType<ITypeKey>
    {
        public TypeKeyType()
        {
            Field(x => x.EntityType).Description("The type of this entity");
            Field(x => x.EntityId).Description("The id of this entity");
        }
    }
}
