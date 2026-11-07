using EarnPlatform.Services.Interfaces;

namespace EarnPlatform.Services.Implementations;

public class ReferralCodeGenerator : IReferralCodeGenerator
{
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // no O/0/I/1 ambiguity

    public string Generate()
    {
        var random = Random.Shared;
        var chars = new char[8];
        for (int i = 0; i < chars.Length; i++)
            chars[i] = Alphabet[random.Next(Alphabet.Length)];
        return new string(chars);
    }
}