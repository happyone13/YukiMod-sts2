namespace BaseLib.Utils
{
    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    internal sealed class PoolAttribute(Type poolType) : Attribute
    {
        public Type PoolType { get; } = poolType;
    }

}
