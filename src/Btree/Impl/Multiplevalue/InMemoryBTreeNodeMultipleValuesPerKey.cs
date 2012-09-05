using NDatabase.Btree.Exception;

namespace NDatabase.Btree.Impl.Multiplevalue
{
    
    public sealed class InMemoryBTreeNodeMultipleValuesPerKey : BTreeNodeMultipleValuesPerKey
    {
        private static int _nextId = 1;

        private IBTreeNode[] _children;
        private int _id;

        private IBTreeNode _parent;

        public InMemoryBTreeNodeMultipleValuesPerKey(IBTree btree) : base(btree)
        {
            _id = _nextId++;
        }

        public override IBTreeNode GetChildAt(int index, bool throwExceptionIfNotExist)
        {
            if (_children[index] == null && throwExceptionIfNotExist)
            {
                throw new BTreeException("Trying to load null child node at index "
                                         + index);
            }
            return _children[index];
        }

        public override IBTreeNode GetParent()
        {
            return _parent;
        }

        public override void SetChildAt(IBTreeNode child, int index)
        {
            _children[index] = child;
            if (child != null)
            {
                child.SetParent(this);
            }
        }

        public override void SetChildAt(IBTreeNode node, int childIndex, int
                                                                             index, bool throwExceptionIfDoesNotExist)
        {
            var childTreeNode = node.GetChildAt(childIndex, throwExceptionIfDoesNotExist);

            _children[index] = childTreeNode;

            if (childTreeNode != null)
                childTreeNode.SetParent(this);
        }

        public override void SetParent(IBTreeNode node)
        {
            _parent = node;
        }

        public override bool HasParent()
        {
            return _parent != null;
        }

        protected override void Init()
        {
            _children = new IBTreeNode[MaxNbChildren];
        }

        public override object GetId()
        {
            return _id;
        }

        public override void SetId(object id)
        {
            _id = (int) id;
        }

        public override void DeleteChildAt(int index)
        {
            _children[index] = null;
            NbChildren--;
        }

        public override void MoveChildFromTo(int sourceIndex, int destinationIndex, bool
                                                                                        throwExceptionIfDoesNotExist)
        {
            if (_children[sourceIndex] == null && throwExceptionIfDoesNotExist)
            {
                var errorMessage = string.Format("Trying to move null child node at index {0}", sourceIndex);
                throw new BTreeException(errorMessage);
            }
            _children[destinationIndex] = _children[sourceIndex];
        }

        public override void SetNullChildAt(int childIndex)
        {
            _children[childIndex] = null;
        }

        public override object GetChildIdAt(int childIndex, bool throwExceptionIfDoesNotExist)
        {
            if (_children[childIndex] == null && throwExceptionIfDoesNotExist)
            {
                throw new BTreeException("Trying to move null child node at index "
                                         + childIndex);
            }
            return _children[childIndex].GetId();
        }

        public override object GetParentId()
        {
            return _id;
        }

        public override object GetValueAsObjectAt(int index)
        {
            return GetValueAt(index);
        }
    }
}
