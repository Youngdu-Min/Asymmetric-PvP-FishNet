public interface IRandomSource
{
    int NextInt(int minInclusive, int maxExclusive);
    float NextFloat();
    float NextFloat(float minInclusive, float maxInclusive);
    bool Chance(float probability01);
    int Next();
}