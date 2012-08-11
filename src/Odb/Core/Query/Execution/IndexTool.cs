using System;
using NDatabase.Odb.Core.Layers.Layer2.Meta;
using NDatabase.Odb.Impl.Core.Query.Criteria;
using NDatabase.Tool.Wrappers;

namespace NDatabase.Odb.Core.Query.Execution
{
    public static class IndexTool
    {
        public static IOdbComparable BuildIndexKey(string indexName, NonNativeObjectInfo oi, int[] fieldIds)
        {
            var keys = new IOdbComparable[fieldIds.Length];

            for (var i = 0; i < fieldIds.Length; i++)
            {
                // Todo : can we assume that the object is a Comparable
                try
                {
                    var aoi = oi.GetAttributeValueFromId(fieldIds[i]);
                    var item = (IComparable) aoi.GetObject();
                    
                    // If the index is on NonNativeObjectInfo, then the key is the oid 
                    // of the object
                    if (aoi.IsNonNativeObject())
                    {
                        var nnoi = (NonNativeObjectInfo) aoi;
                        item = nnoi.GetOid();
                    }

                    keys[i] = new SimpleCompareKey(item);
                }
                catch (Exception)
                {
                    throw new OdbRuntimeException(
                        NDatabaseError.IndexKeysMustImplementComparable.AddParameter(fieldIds[i]).AddParameter(
                            oi.GetAttributeValueFromId(fieldIds[i]).GetType().FullName));
                }
            }

            if (keys.Length == 1)
                return keys[0];
            return new ComposedCompareKey(keys);
        }

        public static IOdbComparable BuildIndexKey(string indexName, AttributeValuesMap values, string[] fields)
        {
            if (fields.Length == 1)
                return new SimpleCompareKey(values.GetComparable(fields[0]));

            var keys = new IOdbComparable[fields.Length];
            for (var i = 0; i < fields.Length; i++)
            {
                // Todo : can we assume that the object is a Comparable
                try
                {
                    var @object = (IComparable) values[fields[i]];
                    
                    keys[i] = new SimpleCompareKey(@object);
                }
                catch (Exception)
                {
                    throw new OdbRuntimeException(
                        NDatabaseError.IndexKeysMustImplementComparable.AddParameter(indexName).AddParameter(fields[i]).
                            AddParameter(values[fields[i]].GetType().FullName));
                }
            }

            var key = new ComposedCompareKey(keys);
            return key;
        }

        /// <summary>
        ///   Take the fields of the index and take value from the query
        /// </summary>
        /// <param name="ci"> The class info involved </param>
        /// <param name="index"> The index </param>
        /// <param name="query"> </param>
        /// <returns> The key of the index </returns>
        public static IOdbComparable ComputeKey(ClassInfo ci, ClassInfoIndex index, CriteriaQuery query)
        {
            var attributesNames = ci.GetAttributeNames(index.GetAttributeIds());
            var values = query.GetCriteria().GetValues();
            return BuildIndexKey(index.GetName(), values, attributesNames);
        }
    }
}
