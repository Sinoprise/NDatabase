using System;
using NDatabase2.Odb.Core.Layers.Layer2.Meta;

namespace NDatabase2.Odb.Core.Query.Criteria.Evaluations
{
    internal class IdentityEvaluation : AEvaluation
    {
        /// <summary>
        ///   For criteria query on objects, we use the oid of the object instead of the object itself.
        /// </summary>
        /// <remarks>
        ///   For criteria query on objects, we use the oid of the object instead of the object itself. 
        ///   So comparison will be done with OID It is faster and avoid the need of the object (class) 
        ///   having to implement Serializable in client server mode
        /// </remarks>
        private readonly OID _oid;

        public IdentityEvaluation(object theObject, string attributeName, IQuery query) 
            : base(theObject, attributeName)
        {
            if (IsNative())
                throw new ArgumentException("Constrain object cannot be native object.");

            // For non native object, we just need the oid of it
            _oid = ((IInternalQuery)query).GetStorageEngine().GetObjectId(theObject, false);
        }


        public override bool Evaluate(object candidate)
        {
            candidate = AsAttributeValuesMapValue(candidate);

            if (candidate == null && TheObject == null && _oid == null)
                return true;
            
            var objectOid = (OID) candidate;
            if (_oid == null)
            {
                return false;
            }

            return _oid.Equals(objectOid);
        }

        public AttributeValuesMap GetValues()
        {
            var map = new AttributeValuesMap();

            if (_oid != null)
                map.SetOid(_oid);
            else
                map.Add(AttributeName, TheObject);

            return map;
        }
    }
}