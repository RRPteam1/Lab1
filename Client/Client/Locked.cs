namespace Pong
{
    //to make var thread safe using simple lock method
    public class Locked<T>
    {
        private T variable;
        private object _lock = new object();

        public T var
        {
            get { lock (_lock) return variable; }
            set { lock (_lock) variable = value; }
        }

        public Locked(T var = default(T)) => this.var = var;
    }
}