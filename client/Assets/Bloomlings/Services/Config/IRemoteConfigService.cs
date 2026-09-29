namespace Bloomlings.Client.Services.Config
{
    /// <summary>
    /// Typed, clamped access to Remote Config (FR-085). Values are always usable: a missing, malformed or out-of-range
    /// remote value falls back to the bundled default or is clamped into the key's range. Read keys from
    /// <see cref="RemoteConfigKeys"/>.
    /// </summary>
    public interface IRemoteConfigService
    {
        int Get(IntKey key);

        bool Get(BoolKey key);

        string Get(StringKey key);
    }
}
