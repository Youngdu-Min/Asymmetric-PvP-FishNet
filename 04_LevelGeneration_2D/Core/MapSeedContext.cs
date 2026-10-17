public sealed class MapSeedContext
{
    public int BaseSeed { get; }
    public string GeneratorVersion { get; }
    public string ConfigVersion { get; }

    private readonly System.Collections.Generic.Dictionary<MapGenerationPass, IRandomSource> _passSources
        = new System.Collections.Generic.Dictionary<MapGenerationPass, IRandomSource>();

    public MapSeedContext(int baseSeed, string generatorVersion, string configVersion)
    {
        BaseSeed = baseSeed;
        GeneratorVersion = generatorVersion;
        ConfigVersion = configVersion;
    }

    public IRandomSource GetSource(MapGenerationPass pass)
    {
        if (!_passSources.TryGetValue(pass, out var source))
        {
            int derivedSeed = DerivePassSeed(BaseSeed, pass);
            source = new SystemRandomSource(derivedSeed);
            _passSources[pass] = source;
        }
        return source;
    }

    private static int DerivePassSeed(int baseSeed, MapGenerationPass pass)
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + baseSeed;
            hash = hash * 31 + (int)pass;
            return hash;
        }
    }
}