namespace NDatabase.Btree.Exception
{
    
    public sealed class DuplicatedKeyException : BTreeException
    {
        public DuplicatedKeyException()
        {
        }

        public DuplicatedKeyException(string message, System.Exception cause) : base(message, cause)
        {
        }

        public DuplicatedKeyException(string message) : base(message)
        {
        }
    }
}