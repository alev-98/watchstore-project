using System.Security.Cryptography;
using System.Text;

namespace WatchStore.Worker.MessageHandlers.Orders;

public class WatchCodeGenerator
{
    private const string characters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    private const int codeLength = 12;
    private const int dashInterval = 4;

    public static List<string> GenerateCodes(int count)
    {
        if (count <= 0)
        {
            return [];
        }

        var codes = new List<string>(count);
        using var generator = RandomNumberGenerator.Create();

        for (int i = 0; i < count; i++)
        {
            codes.Add(GenerateCode(generator));
        }

        return codes;
    }

    private static string GenerateCode(RandomNumberGenerator generator)
    {
        var code = new StringBuilder(codeLength + (codeLength / dashInterval));
        var randomBytes = new byte[4];

        for (int i = 0; i < codeLength; i++)
        {
            if (i > 0 && i % dashInterval == 0)
            {
                code.Append('-');
            }

            generator.GetBytes(randomBytes);
            uint num = BitConverter.ToUInt32(randomBytes, 0);
            code.Append(characters[(int)(num % characters.Length)]);
        }

        return code.ToString();
    }
}
