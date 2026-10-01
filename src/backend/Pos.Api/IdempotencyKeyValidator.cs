using Microsoft.AspNetCore.Http;

namespace Pos.Api;

internal static class IdempotencyKeyValidator
{
    private const string HeaderName = "Idempotency-Key";

    internal static bool TryGetValid(
        IHeaderDictionary headers,
        out string? value)
    {
        value = null;

        if (!headers.TryGetValue(HeaderName, out var values) ||
            values.Count != 1)
        {
            return false;
        }

        var candidate = values[0];
        if (candidate is null or { Length: < 1 or > 128 })
        {
            return false;
        }

        foreach (var character in candidate)
        {
            if (character is < '!' or > '~')
            {
                return false;
            }
        }

        value = candidate;
        return true;
    }
}
