using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace Pos.Api.Sales;

internal static class ConfirmSaleClientOperationAdvisoryKey
{
    internal static long Derive(long branchId, string clientOperationId)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(branchId);
        ArgumentException.ThrowIfNullOrEmpty(clientOperationId);

        // Frozen binary preimage: ASCII domain + NUL + Int64BE + UInt32BE + exact UTF-8.
        ReadOnlySpan<byte> domainSeparator = "POS:CONFIRM_SALE:CLIENT_OPERATION:v1\0"u8;
        var utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
        var clientOperationBytes = utf8.GetBytes(clientOperationId);
        var branchIdOffset = domainSeparator.Length;
        var byteLengthOffset = branchIdOffset + sizeof(long);
        var clientOperationOffset = byteLengthOffset + sizeof(uint);
        var preimage = new byte[checked(clientOperationOffset + clientOperationBytes.Length)];

        domainSeparator.CopyTo(preimage);
        BinaryPrimitives.WriteInt64BigEndian(preimage.AsSpan(branchIdOffset, sizeof(long)), branchId);
        BinaryPrimitives.WriteUInt32BigEndian(
            preimage.AsSpan(byteLengthOffset, sizeof(uint)),
            (uint)clientOperationBytes.Length);
        clientOperationBytes.CopyTo(preimage, clientOperationOffset);

        var digest = SHA256.HashData(preimage);
        return BinaryPrimitives.ReadInt64BigEndian(digest.AsSpan(0, sizeof(long)));
    }
}
