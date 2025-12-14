namespace AvitoSparesParser.Common;

public static class EnumerableExtensions
{
    extension<T>(IEnumerable<T> enumerable)
    {
        public async Task<U[]> MapArrayAsync<U>(Func<T, Task<U>> func)
        {
            T[] array = enumerable.ToArray();
            int length = array.Length;
            U[] mapped = new U[length];
            for (int i = 0; i < array.Length; i++)
            {
                T item = array[i];
                U result = await func(item);
                mapped[i] = result;
            }
            return mapped;
        }

        public async Task InvokeForEach(Func<T, Task> func)
        {
            foreach (T item in enumerable)
            {
                await func(item);
            }
        }
    }
}