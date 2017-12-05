using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using RulEng.States;
using GraphQL.Types;
using Newtonsoft.Json.Linq;
using Redux;

namespace RulEng.GraphModels
{
    public class OperationType : ObjectGraphType<Operation>
    {
        public OperationType()
        {
            Field(x => x.OperationId).Description("Unique ID of the Operation.");
            Field(x => x.RuleResultId).Description("ID of the RuleResult this Operation references.");
            // TODO: check whether these next two are correct
            Field<ListGraphType<OperandKeyType>>("Operands");
            Field<ListGraphType<TypeKeyType>>("TargetEntities");
            Field(x => x.OperationType).Description("The type of operation to be performed against the target(s)");
        }

        public JTokenType ValueType { get; set; }

        public IAction ValueAction { get; set; }

        /// <summary>
        /// Optional value that can be used to Obtain a value (when the data is not present in the store)
        /// </summary>
        public object ValueSource { get; set; }

        /// <summary>
        /// Optional value that defines whether the ValueSource should be used as-is (is literal) or treated as a reference
        /// </summary>
        public bool ValueSourceIsLiteral { get; set; }

        public ImmutableArray<ImmutableArray<Guid>> ReferenceValueIds { get; set; } = ImmutableArray<ImmutableArray<Guid>>.Empty;
    }
    
}
