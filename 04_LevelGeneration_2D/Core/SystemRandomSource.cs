public sealed class SystemRandomSource : IRandomSource
{
    private readonly System.Random _random;

    public SystemRandomSource(int seed)
    {
        _random = new System.Random(seed);
    }

    public int NextInt(int minInclusive, int maxExclusive)
        => _random.Next(minInclusive, maxExclusive);

    public float NextFloat()
        => (float)_random.NextDouble();

    public float NextFloat(float minInclusive, float maxInclusive)
        => minInclusive + (maxInclusive - minInclusive) * NextFloat();

    public bool Chance(float probability01)
        => NextFloat() < probability01;

    public int Next()
        => _random.Next();
}