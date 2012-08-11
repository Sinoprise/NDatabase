using NDatabase.Odb.Core.Layers.Layer2.Instance;
using NDatabase.Odb.Core.Layers.Layer2.Meta;

namespace NDatabase.Odb.Core.Query.Execution
{
    /// <summary>
    ///   Used to implement generic action on matching object.
    /// </summary>
    /// <remarks>
    ///   Used to implement generic action on matching object. The Generic query executor is responsible for checking if an object meets the criteria conditions. Then an(some) object actions are called to execute what must be done with matching objects. A ValuesQuery can contain more than one QueryFieldAction.
    /// </remarks>
    /// <author>osmadja</author>
    public interface IQueryFieldAction
    {
        void Start();

        void End();

        void Execute(OID oid, AttributeValuesMap values);

        object GetValue();

        string GetAttributeName();

        string GetAlias();

        /// <summary>
        ///   To indicate if a query will return one row (for example, sum, average, max and min, or will return more than one row
        /// </summary>
        bool IsMultiRow();

        void SetMultiRow(bool isMultiRow);

        /// <summary>
        ///   used to create a copy!
        /// </summary>
        IQueryFieldAction Copy();

        void SetInstanceBuilder(IInstanceBuilder builder);

        IInstanceBuilder GetInstanceBuilder();

        /// <param name="returnInstance"> </param>
        void SetReturnInstance(bool returnInstance);

        bool ReturnInstance();
    }
}