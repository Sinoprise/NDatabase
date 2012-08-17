using System;
using System.Text;
using NDatabase.Odb.Impl.Core.Layers.Layer3.Engine;
using NDatabase.Tool.Wrappers;

namespace NDatabase.Odb.Core.Layers.Layer2.Meta
{
    /// <summary>
    ///   Some basic info about an object info like position, its class info,...
    /// </summary>
    [Serializable]
    public sealed class ObjectInfoHeader
    {
        private int[] _attributeIds;

        /// <summary>
        ///   Can be position(for native object) or id(for non native object, positions are positive e ids are negative
        /// </summary>
        private long[] _attributesIdentification;

        private OID _classInfoId;
        private long _creationDate;
        private OID _nextObjectOID;
        private int _objectVersion;
        private OID _oid;
        private long _position;
        private OID _previousObjectOID;
        private long _updateDate;

        public ObjectInfoHeader(long position, OID previousObjectOID, OID nextObjectOID, OID classInfoId,
                                long[] attributesIdentification, int[] attributeIds)
        {
            _position = position;
            _oid = null;
            _previousObjectOID = previousObjectOID;
            _nextObjectOID = nextObjectOID;
            _classInfoId = classInfoId;
            _attributesIdentification = attributesIdentification;
            _attributeIds = attributeIds;
            _objectVersion = 1;
            _creationDate = OdbTime.GetCurrentTimeInTicks();
        }

        public ObjectInfoHeader()
        {
            _position = -1;
            _oid = null;
            _objectVersion = 1;
            _creationDate = OdbTime.GetCurrentTimeInTicks();
        }

        public int GetNbAttributes()
        {
            return _attributesIdentification.Length;
        }

        public OID GetNextObjectOID()
        {
            return _nextObjectOID;
        }

        public void SetNextObjectOID(OID nextObjectOID)
        {
            _nextObjectOID = nextObjectOID;
        }

        public long GetPosition()
        {
            return _position;
        }

        public void SetPosition(long position)
        {
            _position = position;
        }

        //	/**
        //     * @return Returns the classInfoId.
        //     */
        //    public long getClassInfoId() {
        //        return classInfoId;
        //    }
        //    /**
        //     * @param classInfoId The classInfoId to set.
        //     */
        //    public void setClassInfoId(long classInfoId) {
        //        this.classInfoId = classInfoId;
        //    }
        public OID GetPreviousObjectOID()
        {
            return _previousObjectOID;
        }

        public void SetPreviousObjectOID(OID previousObjectOID)
        {
            _previousObjectOID = previousObjectOID;
        }

        public OID GetClassInfoId()
        {
            return _classInfoId;
        }

        public override string ToString()
        {
            var buffer = new StringBuilder();
            buffer.Append("oid=").Append(_oid);
            buffer.Append(" - class info id=").Append(_classInfoId);
            buffer.Append(" - position=").Append(_position).Append(" | prev=").Append(_previousObjectOID);
            buffer.Append(" | next=").Append(_nextObjectOID);
            buffer.Append(" attrs =[");
            if (_attributesIdentification != null)
            {
                foreach (var value in _attributesIdentification)
                    buffer.Append(value).Append(" ");
            }
            else
            {
                buffer.Append(" nulls ");
            }

            buffer.Append(" ]");
            return buffer.ToString();
        }

        public long[] GetAttributesIdentification()
        {
            return _attributesIdentification;
        }

        public void SetAttributesIdentification(long[] attributesIdentification)
        {
            _attributesIdentification = attributesIdentification;
        }

        public OID GetOid()
        {
            return _oid;
        }

        public void SetOid(OID oid)
        {
            _oid = oid;
        }

        public long GetCreationDate()
        {
            return _creationDate;
        }

        public void SetCreationDate(long creationDate)
        {
            _creationDate = creationDate;
        }

        public long GetUpdateDate()
        {
            return _updateDate;
        }

        public void SetUpdateDate(long updateDate)
        {
            _updateDate = updateDate;
        }

        /// <summary>
        ///   Return the attribute identification (position or id) from the attribute id FIXME Remove dependency from StorageEngineConstant
        /// </summary>
        /// <param name="attributeId"> </param>
        /// <returns> -1 if attribute with this id does not exist </returns>
        public long GetAttributeIdentificationFromId(int attributeId)
        {
            if (_attributeIds == null)
                return StorageEngineConstant.NullObjectIdId;

            for (var i = 0; i < _attributeIds.Length; i++)
            {
                if (_attributeIds[i] == attributeId)
                    return _attributesIdentification[i];
            }

            return StorageEngineConstant.NullObjectIdId;
        }

        public long GetAttributeId(int attributeIndex)
        {
            return _attributeIds[attributeIndex];
        }

        public void SetAttributesIds(int[] ids)
        {
            _attributeIds = ids;
        }

        public int[] GetAttributeIds()
        {
            return _attributeIds;
        }

        public void SetClassInfoId(OID classInfoId2)
        {
            _classInfoId = classInfoId2;
        }

        public int GetObjectVersion()
        {
            return _objectVersion;
        }

        public void SetObjectVersion(int objectVersion)
        {
            _objectVersion = objectVersion;
        }

        public override int GetHashCode()
        {
            var result = 1;
            result = 31 * result + (int) (_position ^ ((_position) >> (32 & 0x1f)));
            return result;
        }

        public override bool Equals(object obj)
        {
            if (this == obj)
                return true;

            if (obj == null)
                return false;

            if (GetType() != obj.GetType())
                return false;

            var other = (ObjectInfoHeader) obj;
            
            if (_position != other._position)
                return false;
            return true;
        }

        public void IncrementVersionAndUpdateDate()
        {
            _objectVersion++;
            _updateDate = OdbTime.GetCurrentTimeInTicks();
        }

        public ObjectInfoHeader Duplicate()
        {
            var objectInfoHeader = new ObjectInfoHeader();

            objectInfoHeader.SetAttributesIdentification(_attributesIdentification);
            objectInfoHeader.SetAttributesIds(_attributeIds);
            objectInfoHeader.SetClassInfoId(_classInfoId);
            objectInfoHeader.SetCreationDate(_creationDate);
            objectInfoHeader.SetNextObjectOID(_nextObjectOID);
            objectInfoHeader.SetObjectVersion(_objectVersion);
            objectInfoHeader.SetOid(_oid);
            objectInfoHeader.SetPosition(_position);
            objectInfoHeader.SetPreviousObjectOID(_previousObjectOID);
            objectInfoHeader.SetUpdateDate(_updateDate);

            return objectInfoHeader;
        }
    }
}
