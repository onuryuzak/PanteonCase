namespace Panteon.Core
{
    public interface IPoolable
    {
        void OnTakenFromPool();
        void OnReturnedToPool();
    }
}

