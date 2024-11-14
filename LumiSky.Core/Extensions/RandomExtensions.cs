namespace LumiSky.Core.Extensions;

public static class RandomExtensions
{
    /// <summary>
    /// Generates random numbers without replacement.
    /// </summary>
    /// <param name="rand"></param>
    /// <param name="max"></param>
    /// <param name="count"></param>
    /// <returns></returns>
    public static int[] RandomSample(this Random rand, int max, int count)
    {
        HashSet<int> numbers = new(count);
        for (int i = 0; i < count; i++)
            while (!numbers.Add(rand.Next(0, max))) ;
        return numbers.ToArray();
    }
}
