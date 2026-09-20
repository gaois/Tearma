using Ixnas.AltchaNet;

namespace TearmaWeb.Controllers;

public class AltchaChallengeStore : IAltchaChallengeStore
{
    private static IAltchaChallengeStore _instance = new AltchaChallengeStore();
    public static IAltchaChallengeStore GetInstance()
    {
        return _instance;
    }

    private class StoredChallenge
    {
        public string Challenge { get; set; }
        public DateTimeOffset ExpiryUtc { get; set; }
    }

    private readonly List<StoredChallenge> _stored = new List<StoredChallenge>();

    public Task Store(string challenge, DateTimeOffset expiryUtc)
    {
        var challengeToStore = new StoredChallenge
        {
            Challenge = challenge,
            ExpiryUtc = expiryUtc
        };
        _stored.Add(challengeToStore);
        return Task.CompletedTask;
    }

    public Task<bool> Exists(string challenge)
    {
        _stored.RemoveAll(storedChallenge => storedChallenge.ExpiryUtc <= DateTimeOffset.UtcNow);
        var exists = _stored.Exists(storedChallenge => storedChallenge.Challenge == challenge);
        return Task.FromResult(exists);
    }
}