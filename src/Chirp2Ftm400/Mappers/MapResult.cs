namespace Chirp2Ftm400.Mappers;

public record MapResult<T>(T Value, IReadOnlyList<string> Warnings)
{
    public static MapResult<T> Ok(T value) => new(value, []);
    public static MapResult<T> Warn(T value, string warning) => new(value, [warning]);
}
