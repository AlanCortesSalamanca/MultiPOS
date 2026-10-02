using System.Security.Cryptography;

namespace Pos.Api.Sales;

internal static class ConfirmSaleRequestHasher
{
    internal static string Compute(ConfirmSaleRequest request)
    {
        var canonicalBytes = ConfirmSaleCanonicalRequestSerializer.Serialize(request);
        var digest = SHA256.HashData(canonicalBytes);

        return "sha256:" + Convert.ToHexStringLower(digest);
    }
}
