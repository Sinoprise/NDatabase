using System;

namespace NDatabase.Odb.Core.Query.NQ
{
    [Serializable]
    public class SimpleNativeQuery : AbstractQuery
    {
        public override void SetFullClassName(Type type)
        {
            // nothing
        }
    }
}