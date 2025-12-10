namespace Tests.ParsingTests.Common;

public static class TextListConstruction
{
    extension(TextList)
    {
        public static TextList Empty()
        {
            return new TextList([]);
        }

        public static async Task<TextList> FromEmptyWithAdditionsAsync(
            params Func<TextList, Task<TextList>>[] fillingFns
            )
        {
            TextList empty = Empty();
            foreach (Func<TextList, Task<TextList>> fn in fillingFns)
                empty = await fn(empty);
            return empty;
        }

        public static async Task<TextList> FromRequirementsAsync<T>(T source, Func<T, Task<TextList>>[] requirements)
        {
            TextList textList = TextList.Empty();
            foreach (Func<T, Task<TextList>> requirement in requirements) 
                textList.Add(await requirement(source));
            return textList;
        }
    }
}