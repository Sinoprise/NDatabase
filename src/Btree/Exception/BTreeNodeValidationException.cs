namespace NDatabase.Btree.Exception
{
    
    public sealed class BTreeNodeValidationException : System.Exception
    {
        public BTreeNodeValidationException()
        {
        }

        public BTreeNodeValidationException(string message, System.Exception cause) : base(message, cause)
        {
        }

        public BTreeNodeValidationException(string message) : base(message)
        {
        }
    }
}