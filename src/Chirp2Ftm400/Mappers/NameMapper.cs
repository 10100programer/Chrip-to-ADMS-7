namespace Chirp2Ftm400.Mappers;

public static class NameMapper
{
    private const int MaxLength = 8;

    public static MapResult<string> Map(string? chirpName)
    {
        if (chirpName is null)
            return MapResult<string>.Ok("");

        var warnings = new List<string>();

        // Replace non-ASCII or non-printable characters with space
        char[] chars = chirpName.ToCharArray();
        bool hadInvalid = false;
        for (int i = 0; i < chars.Length; i++)
        {
            char c = chars[i];
            if (c > 127 || c < 32)
            {
                chars[i] = ' ';
                hadInvalid = true;
            }
        }

        string cleaned = new string(chars);

        if (hadInvalid)
            warnings.Add($"Name '{chirpName}' contained non-ASCII or non-printable characters; replaced with spaces");

        string result = cleaned;
        if (result.Length > MaxLength)
        {
            result = result[..MaxLength];
            warnings.Add($"Name '{cleaned}' truncated to '{result}' (max {MaxLength} chars)");
        }

        if (warnings.Count == 0)
            return MapResult<string>.Ok(result);

        return new MapResult<string>(result, warnings);
    }
}
