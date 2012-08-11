using System;
using System.Text;
using NDatabase.Odb.Core;
using NDatabase.Odb.Core.Layers.Layer2.Meta;
using NDatabase.Odb.Core.Layers.Layer3.Engine;
using NDatabase.Odb.Core.Transaction;
using NDatabase.Tool;
using NDatabase.Tool.Wrappers.List;

namespace NDatabase.Odb.Impl.Core.Transaction
{
    /// <summary>
    ///   The WriteAction class is the description of a Write operation that will be applied to the main database file when committing.
    /// </summary>
    /// <remarks>
    ///   The WriteAction class is the description of a Write operation that will be applied to the main database file when committing. All operations(writes) that can not be written to the database file before committing , pointers (for example) are stored in WriteAction objects. The transaction keeps track of all these WriteActions. When committing, the transaction apply each WriteAction to the engine database file.
    /// </remarks>
    /// <author>osmadja</author>
    public class DefaultWriteAction : IWriteAction
    {
        public const int UnknownWriteAction = 0;
        public const int DataWriteAction = 1;
        public const int PointerWriteAction = 2;
        public const int DirectWriteAction = 3;

        public static readonly string LogId = "WriteAction";

        private readonly IByteArrayConverter _byteArrayConverter;

        private IOdbList<byte[]> _listOfBytes;
        private long _position;

        private int _size;

        public DefaultWriteAction(long position) : this(position, null)
        {
        }

        public DefaultWriteAction(long position, byte[] bytes) : this(position, bytes, null)
        {
        }

        public DefaultWriteAction(long position, byte[] bytes, string label)
        {
            _byteArrayConverter = OdbConfiguration.GetCoreProvider().GetByteArrayConverter();
            _position = position;
            //TODO:perf should init with no default size?
            _listOfBytes = new OdbArrayList<byte[]>(20);
            if (bytes != null)
            {
                _listOfBytes.Add(bytes);
                _size = bytes.Length;
            }
        }

        #region IWriteAction Members

        public virtual long GetPosition()
        {
            return _position;
        }

        public virtual byte[] GetBytes(int index)
        {
            return _listOfBytes[index];
        }

        public virtual void AddBytes(byte[] bytes)
        {
            _listOfBytes.Add(bytes);
            _size += bytes.Length;
        }

        public virtual void Persist(IFileSystemInterface fsi, int index)
        {
            var currentPosition = fsi.GetPosition();
            
            // DLogger.debug("# Writing WriteAction #" + index + " at " +
            // currentPosition+" : " + toString());
            var sizeOfLong = OdbType.Long.GetSize();
            var sizeOfInt = OdbType.Integer.GetSize();

            // build the full byte array to write once
            var bytes = new byte[sizeOfLong + sizeOfInt + _size];

            var bytesOfPosition = _byteArrayConverter.LongToByteArray(_position);
            var bytesOfSize = _byteArrayConverter.IntToByteArray(_size);
            for (var i = 0; i < sizeOfLong; i++)
                bytes[i] = bytesOfPosition[i];

            var offset = sizeOfLong;
            for (var i = 0; i < sizeOfInt; i++)
            {
                bytes[offset] = bytesOfSize[i];
                offset++;
            }

            foreach (var tmp in _listOfBytes)
            {
                Array.Copy(tmp, 0, bytes, offset, tmp.Length);
                offset += tmp.Length;
            }

            fsi.WriteBytes(bytes, false, "Transaction");
            var fixedSize = sizeOfLong + sizeOfInt;
            var positionAfterWrite = fsi.GetPosition();
            var writeSize = positionAfterWrite - currentPosition;

            if (writeSize != _size + fixedSize)
                throw new OdbRuntimeException(
                    NDatabaseError.DifferentSizeInWriteAction.AddParameter(_size).AddParameter(writeSize));
        }

        public virtual void ApplyTo(IFileSystemInterface fsi, int index)
        {
            if (OdbConfiguration.IsDebugEnabled(LogId))
                DLogger.Debug(string.Format("Applying WriteAction #{0} : {1}", index, ToString()));

            fsi.SetWritePosition(_position, false);
            for (var i = 0; i < _listOfBytes.Count; i++)
                fsi.WriteBytes(GetBytes(i), false, "WriteAction");
        }

        public virtual bool IsEmpty()
        {
            return _listOfBytes == null || _listOfBytes.IsEmpty();
        }

        #endregion

        public virtual void SetPosition(long position)
        {
            _position = position;
        }

        public static DefaultWriteAction Read(IFileSystemInterface fsi, int index)
        {
            try
            {
                var position = fsi.ReadLong();
                var size = fsi.ReadInt();
                var bytes = fsi.ReadBytes(size);
                var writeAction = new DefaultWriteAction(position, bytes);

                if (OdbConfiguration.IsDebugEnabled(LogId))
                    DLogger.Debug(string.Format("Loading Write Action # {0} at {1} => {2}", index, fsi.GetPosition(), writeAction));

                return writeAction;
            }
            catch (OdbRuntimeException)
            {
                DLogger.Error(string.Format("error reading write action {0} at position {1}", index, fsi.GetPosition()));
                throw;
            }
        }

        public override string ToString()
        {
            var buffer = new StringBuilder();
            buffer.Append("position=").Append(_position);
            var bytes = new StringBuilder();

            if (_listOfBytes != null)
            {
                for (var i = 0; i < _listOfBytes.Count; i++)
                    bytes.Append(DisplayUtility.ByteArrayToString(GetBytes(i)));

                buffer.Append(" | bytes=[").Append(bytes).Append("] & size=" + _size);
            }
            else
                buffer.Append(" | bytes=null & size=").Append(_size);

            return buffer.ToString();
        }

        public virtual void Clear()
        {
            _listOfBytes.Clear();
            _listOfBytes = null;
        }
    }
}