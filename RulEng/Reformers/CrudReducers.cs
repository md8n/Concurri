using System.Collections.Immutable;
using System.Linq;
using RulEng.Prescriptions;
using RulEng.States;

namespace RulEng.Reformers
{
    public static class CrudReducers
    {
        public static ImmutableHashSet<T> CrudReducer<T>(ImmutableHashSet<T> state, ICrud prescription) where T: IEntity
        {
            switch (prescription)
            {
                case Create<T> createEntityPrescription:
                    return CreateEntityReducer(state, createEntityPrescription);
                case Delete<T> deleteEntityPrescription:
                    return DeleteEntityReducer(state, deleteEntityPrescription);
            }

            return state;
        }

        public static ImmutableHashSet<T> CreateEntityReducer<T>(ImmutableHashSet<T> previousState, Create<T> prescription) where T : IEntity
        {
            return previousState.Add(prescription.Entity);
        }

        public static ImmutableHashSet<T> DeleteEntityReducer<T>(ImmutableHashSet<T> previousState, Delete<T> prescription) where T : IEntity
        {
            var entityToDelete = previousState.First(a => a.EntityId == prescription.EntityId);

            return previousState.Remove(entityToDelete);
        }
    }
}
