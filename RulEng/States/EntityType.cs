using System.Linq;

namespace RulEng.States
{
    public enum EntityType
    {
        Error = -1,
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
        private static readonly EntityType[] ProcessableEntityTypes = { EntityType.Rule, EntityType.Operation, EntityType.Request, EntityType.Value };

        public static bool IsProcessable<T>(this T entity) where T: IEntity
        {
            return ProcessableEntityTypes.Contains(entity.EntType);
        }

        public static bool IsProcessable(this EntityType entType)
        {
            return ProcessableEntityTypes.Contains(entType);
        }
    }
}
