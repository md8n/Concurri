using System.Linq;

namespace RulEng.States
{
    public enum EntityType
    {
        Unknown = 0,
        Rule = 1,

        /// <summary>
        /// RuleResult entities are for the exclusive use of Rules
        /// </summary>
        RuleResult,
        Operation,
        Request,
        Value
    }

    public static class EntityTypeHelpers {
        public static readonly EntityType[] ProcessableEntityTypes = new EntityType[] { EntityType.Rule, EntityType.Operation, EntityType.Request, EntityType.Value };

        public static bool IsProcessable<T>(this T entity) where T: IEntity
        {
            return ProcessableEntityTypes.Contains(entity.Type);
        }
    }
}
