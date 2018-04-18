using System;
using System.Collections.Generic;

namespace RulEng.States
{
    public interface IEntity
    {
        /// <summary>
        /// Id of this Entity
        /// </summary>
        Guid EntityId { get; set; }

        /// <summary>
        /// EntityType of this Entity
        /// </summary>
        EntityType EntType { get; }

        /// <summary>
        /// Tags associated with this Entity
        /// </summary>
        List<string> EntTags { get; set; }

        /// <summary>
        /// Date that this Entity was Last Changed
        /// </summary>
        DateTime LastChanged { get; set; }
    }
}
