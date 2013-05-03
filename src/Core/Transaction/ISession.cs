using System;
using NDatabase.Core.Layers.Layer2.Meta;
using NDatabase.Core.Layers.Layer3;

namespace NDatabase.Core.Transaction
{
    internal interface ISession : IComparable
    {
        IOdbCache GetCache();

        IReadObjectsCache GetTmpCache();

        void Rollback();

        void Close();

        bool IsRollbacked();

        IStorageEngine GetStorageEngine();

        bool TransactionIsPending();

        void Commit();

        ITransaction GetTransaction();

        void SetFileSystemInterfaceToApplyTransaction(IFileSystemInterface fsi);

        IMetaModel GetMetaModel();

        string GetId();

        void RemoveObjectFromCache(object @object);

        IObjectWriter GetObjectWriter();
    }
}