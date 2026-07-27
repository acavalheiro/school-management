using System.Security.Cryptography;

namespace Infrastructure.Identity;

// Generates a random temporary password that satisfies the configured Identity
// password policy (see AddInfrastructure): >= 8 chars, at least one uppercase,
// one lowercase and one digit. A symbol is included for strength even though the
// policy does not require one. Uses a cryptographic RNG, not System.Random.
internal static class PasswordGenerator
{
    // Ambiguous characters (O/0, I/l/1) are omitted so a relayed password is easy to read.
    private const string Upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
    private const string Lower = "abcdefghijkmnpqrstuvwxyz";
    private const string Digits = "23456789";
    private const string Symbols = "!@#$%*-_?";
    private const string All = Upper + Lower + Digits + Symbols;

    public static string Generate(int length = 16)
    {
        if (length < 8)
            length = 8;

        var chars = new char[length];

        // Guarantee one character from each required class up front...
        chars[0] = Upper[RandomNumberGenerator.GetInt32(Upper.Length)];
        chars[1] = Lower[RandomNumberGenerator.GetInt32(Lower.Length)];
        chars[2] = Digits[RandomNumberGenerator.GetInt32(Digits.Length)];
        chars[3] = Symbols[RandomNumberGenerator.GetInt32(Symbols.Length)];

        for (var i = 4; i < length; i++)
            chars[i] = All[RandomNumberGenerator.GetInt32(All.Length)];

        // ...then shuffle so those guaranteed characters are not always in fixed positions.
        for (var i = length - 1; i > 0; i--)
        {
            var j = RandomNumberGenerator.GetInt32(i + 1);
            (chars[i], chars[j]) = (chars[j], chars[i]);
        }

        return new string(chars);
    }
}
