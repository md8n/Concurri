﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using RulEng.States;
using RulEng.Prescriptions;

namespace RulEng.Helpers
{
    public static class RulEngHelpers
    {
        /// <summary>
        /// For a given Rule and RulePrescription create
        /// A RuleResult to accept the result of the Rule
        /// and ensure that the RuleResultId is the same for the Rule, RuleResult and RulePrescription
        /// </summary>
        /// <param name="rule"></param>
        /// <param name="rulePrescription"></param>
        /// <returns></returns>
        public static RuleResult UnifyRuleObjects(this Rule rule, IRuleProcessing rulePrescription)
        {
            // Create the Rule, RuleResult and RulePrescription and ensure that the RuleResultId is the same for all
            var ruleResult = new RuleResult(rule)
            {
                RuleResultId = rulePrescription.Entities.RuleResultId
            };
            rule.ReferenceValues.RuleResultId = rulePrescription.Entities.RuleResultId;

            return ruleResult;
        }

        /// <summary>
        /// For a given entity (Rule, Operation, Request, Value) this creates:
        /// A (not) Exists Rule to test for its presence
        /// A RuleResult to accept the result of the Exists Rule
        /// A RulePrescription referencing the Rule to be performed
        /// </summary>
        /// <param name="val"></param>
        /// <param name="negateResult"></param>
        /// <returns></returns>
        public static (Rule rule, RuleResult ruleResult, IRuleProcessing ruleProcessing) Exists<T>(this T val, bool negateResult) where T : IEntity
        {
            if (!val.IsProcessable())
            {
                throw new ArgumentOutOfRangeException(nameof(val), "Exists helper creator is only for Processable entity types");
            }

            var vType = new TypeKey { EntityId = val.EntityId, EntType = val.EntType, LastChanged = val.LastChanged };

            // Create the Rule, RuleResult and RulePrescription and ensure that the RuleResultId is the same for all
            var rule = vType.ExistsRule(negateResult);
            var rulePrescription = rule.Exists();
            var ruleResult = rule.UnifyRuleObjects(rulePrescription);

            return (rule, ruleResult, rulePrescription);
        }

        /// <summary>
        /// For a given OperandKey (from an Operation / Request) this creates:
        /// A (not) Exists Rule to test for the presence of the entity to be created / updated
        /// A RuleResult to accept the result of the Exists Rule
        /// A RulePrescription referencing the Rule to be performed
        /// </summary>
        /// <param name="val"></param>
        /// <param name="negateResult"></param>
        /// <returns></returns>
        public static (Rule rule, RuleResult ruleResult, IRuleProcessing ruleProcessing) Exists<T>(this OperandKey val, bool negateResult) where T : IEntity
        {
            var vType = new TypeKey { EntityId = val.EntityId, EntType = val.EntType, LastChanged = val.LastChanged };

            // Create the Rule, RuleResult and RulePrescription and ensure that the RuleResultId is the same for all
            var rule = vType.ExistsRule(negateResult);
            var rulePrescription = rule.Exists();
            var ruleResult = rule.UnifyRuleObjects(rulePrescription);

            return (rule, ruleResult, rulePrescription);
        }

        /// <summary>
        /// For a given list of OperandKeys (from an Operation / Request) this creates:
        /// A list of (not) Exists Rules to test for the presence of the entities to be created / updated
        /// A list of RuleResults to accept the result of the Exists Rules
        /// A list of RulePrescriptions referencing the Rules to be performed
        /// </summary>
        /// <param name="vals"></param>
        /// <param name="negateResult"></param>
        /// <returns></returns>
        public static (List<Rule> rules, List<RuleResult> ruleResults, List<IRuleProcessing> rulePrescriptions) Exists(this IEnumerable<OperandKey> vals, bool negateResult)
        {
            var rules = new List<Rule>();
            var ruleResults = new List<RuleResult>();
            var rulePrescriptions = new List<IRuleProcessing>();
            foreach (var distOpKey in vals)
            {
                (var rule, var ruleResult, var rulePrescription) = distOpKey.Exists(negateResult);
                rules.Add(rule);
                ruleResults.Add(ruleResult);
                rulePrescriptions.Add(rulePrescription);
            }

            return (rules, ruleResults, rulePrescriptions);
        }

    /// <summary>
    /// For a given Value this creates:
    /// A HasMeaningfulValue Rule to test its value
    /// A RuleResult to accept the result of the HasMeaningfulValue Rule
    /// A RulePrescription referencing the Rule to be performed
    /// </summary>
    /// <param name="val"></param>
    /// <param name="negateResult"></param>
    /// <returns></returns>
    public static (Rule rule, RuleResult ruleResult, IRuleProcessing rulePrescription) HasMeaningfulValue(this Value val, bool negateResult)
        {
            // Create the Rule, RuleResult and RulePrescription and ensure that the RuleResultId is the same for all
            var rule = val.HasMeaningfulValueRule(negateResult);
            var rulePrescription = rule.HasMeaningfulValue();
            var ruleResult = rule.UnifyRuleObjects(rulePrescription);

            return (rule, ruleResult, rulePrescription);
        }

        /// <summary>
        /// For a given collection of RuleResults this creates:
        /// An And Rule to test their values
        /// A RuleResult to accept the result of the And Rule
        /// A RulePrescription referencing the Rule to be performed
        /// </summary>
        /// <param name="ruleResults"></param>
        /// <param name="negateResult"></param>
        /// <returns></returns>
        public static (Rule rule, RuleResult ruleResult, IRuleProcessing rulePrescription) And(this List<RuleResult> ruleResults, bool negateResult)
        {
            // Create the Rule, RuleResult and RulePrescription and ensure that the RuleResultId is the same for all
            var rule = ruleResults.AndRule(negateResult);
            var rulePrescription = rule.And();
            var ruleResult = rule.UnifyRuleObjects(rulePrescription);

            return (rule, ruleResult, rulePrescription);
        }

        /// <summary>
        /// For a given Rule this creates:
        /// A RuleResult to accept the result of the Rule
        /// A RulePrescription referencing the Rule to be performed
        /// </summary>
        /// <param name="rule"></param>
        /// <returns></returns>
        public static (RuleResult ruleResult, T rulePrescription) ResultAndPrescription<T>(this Rule rule) where T : IRulePrescription, new()
        {
            var rulePrescription = rule.RulePrescription<T>();
            var ruleResult = new RuleResult(rule)
            {
                RuleResultId = rulePrescription.RuleResultId
            };

            rule.ReferenceValues.RuleResultId = rulePrescription.RuleResultId;

            return (ruleResult, rulePrescription);
        }

        public static (Operation operation, IEnumerable<Value> value, OperationMxProcessing operationPrescription) Add(this RuleResult ruleResult, IEnumerable<IEnumerable<Guid>> valueIds)
        {
            var values = new List<Value>();
            var vOpers = (from vSet in valueIds.ToArray()
                let value = new Value(0)
                select new OperandKey
                {
                    SourceValueIds = ImmutableArray.Create(vSet.ToArray()),
                    EntityId = value.ValueId,
                    EntType = EntityType.Value
                })
                .ToArray();

            var operation = ruleResult.CreateOperation(vOpers);
            var operationPrescription =
                new OperationMxProcessing {Entities = ImmutableArray.Create(vOpers)};

            return (operation, values, operationPrescription);
        }
    }
}
