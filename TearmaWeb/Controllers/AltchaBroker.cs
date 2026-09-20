using Azure.Core;
using BotDetect.Web;
using Ixnas.AltchaNet;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;

namespace TearmaWeb.Controllers;

public class AltchaBroker(IConfiguration config, IHttpClientFactory httpClientFactory)
{
    private static byte[] GenerateRandomKey()
    {
        var key = new byte[64];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(key);
        }
        return key;
    }

    private AltchaService altchaService = Altcha.CreateService(new AltchaSha256Configuration
    {
        Key = AltchaKey.FromBytes(GenerateRandomKey()),
        StoreFactory = AltchaChallengeStore.GetInstance,
    });

    public AltchaChallenge generateChallenge()
    {
        AltchaChallenge challenge = altchaService.Generate();
        return challenge;
    }

    public async Task<bool> verifyChallenge(HttpRequest request){
        bool ret = false;
        try
        {
            if (request.Cookies.TryGetValue("altcha", out var altcha) && !string.IsNullOrWhiteSpace(altcha))
            {
                AltchaValidationResult validationResult = await altchaService.Validate(altcha);
                if(validationResult.IsValid) ret = true;
            }
        } catch(Exception ex) {}

        return ret;
    }
}
