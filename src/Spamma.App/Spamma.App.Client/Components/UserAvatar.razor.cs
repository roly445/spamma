using System.Text;
using Microsoft.AspNetCore.Components.Authorization;
using Spamma.App.Client.Infrastructure.Auth;

namespace Spamma.App.Client.Components;

/// <summary>
/// Code-behind for the user avatar component.
/// </summary>
public partial class UserAvatar(AuthenticationStateProvider authenticationStateProvider)
{
    private string? _gravatarUrl;
    private string _userInitials = "U";

    protected override async Task OnInitializedAsync()
    {
        var authState = await authenticationStateProvider.GetAuthenticationStateAsync();
        var user = authState.ToUserAuthInfo();

        if (user.IsAuthenticated)
        {
            if (!string.IsNullOrEmpty(user.Name))
            {
                var parts = user.Name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                this._userInitials = parts.Length > 1
                    ? $"{char.ToUpper(parts[0][0])}{char.ToUpper(parts[^1][0])}"
                    : $"{char.ToUpper(parts[0][0])}";
            }
            else if (!string.IsNullOrEmpty(user.EmailAddress))
            {
                this._userInitials = user.EmailAddress[0].ToString().ToUpper();
            }

            if (!string.IsNullOrEmpty(user.EmailAddress))
            {
                var hash = ComputeGravatarHash(user.EmailAddress);
                this._gravatarUrl = $"https://www.gravatar.com/avatar/{hash}?s=32&d=404";
            }
        }
    }

    private static string ComputeGravatarHash(string email)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var hashBytes = Md5.Hash(Encoding.UTF8.GetBytes(normalizedEmail));

        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    private void ShowInitials()
    {
        this._gravatarUrl = null;
        this.StateHasChanged();
    }

    private static class Md5
    {
        private static readonly uint[] ShiftAmounts =
        [
            7, 12, 17, 22, 7, 12, 17, 22, 7, 12, 17, 22, 7, 12, 17, 22,
            5, 9, 14, 20, 5, 9, 14, 20, 5, 9, 14, 20, 5, 9, 14, 20,
            4, 11, 16, 23, 4, 11, 16, 23, 4, 11, 16, 23, 4, 11, 16, 23,
            6, 10, 15, 21, 6, 10, 15, 21, 6, 10, 15, 21, 6, 10, 15, 21,
        ];

        private static readonly uint[] Table =
            Enumerable.Range(0, 64)
                .Select(i => (uint)(Math.Abs(Math.Sin(i + 1)) * 4294967296d))
                .ToArray();

        public static byte[] Hash(byte[] input)
        {
            var padded = Pad(input);
            var a = 0x67452301u;
            var b = 0xefcdab89u;
            var c = 0x98badcfeu;
            var d = 0x10325476u;

            for (var offset = 0; offset < padded.Length; offset += 64)
            {
                var words = new uint[16];
                for (var i = 0; i < 16; i++)
                {
                    words[i] = BitConverter.ToUInt32(padded, offset + (i * 4));
                }

                var originalA = a;
                var originalB = b;
                var originalC = c;
                var originalD = d;

                for (var i = 0; i < 64; i++)
                {
                    uint f;
                    int g;

                    if (i < 16)
                    {
                        f = (b & c) | (~b & d);
                        g = i;
                    }
                    else if (i < 32)
                    {
                        f = (d & b) | (~d & c);
                        g = ((5 * i) + 1) % 16;
                    }
                    else if (i < 48)
                    {
                        f = b ^ c ^ d;
                        g = ((3 * i) + 5) % 16;
                    }
                    else
                    {
                        f = c ^ (b | ~d);
                        g = (7 * i) % 16;
                    }

                    var temp = d;
                    d = c;
                    c = b;
                    b += RotateLeft(a + f + Table[i] + words[g], (int)ShiftAmounts[i]);
                    a = temp;
                }

                a += originalA;
                b += originalB;
                c += originalC;
                d += originalD;
            }

            var result = new byte[16];
            WriteLittleEndian(result, 0, a);
            WriteLittleEndian(result, 4, b);
            WriteLittleEndian(result, 8, c);
            WriteLittleEndian(result, 12, d);

            return result;
        }

        private static byte[] Pad(byte[] input)
        {
            var bitLength = (ulong)input.Length * 8;
            var paddedLength = input.Length + 1;
            while ((paddedLength % 64) != 56)
            {
                paddedLength++;
            }

            var padded = new byte[paddedLength + 8];
            Array.Copy(input, padded, input.Length);
            padded[input.Length] = 0x80;

            for (var i = 0; i < 8; i++)
            {
                padded[paddedLength + i] = (byte)(bitLength >> (8 * i));
            }

            return padded;
        }

        private static uint RotateLeft(uint value, int count)
            => (value << count) | (value >> (32 - count));

        private static void WriteLittleEndian(byte[] buffer, int offset, uint value)
        {
            buffer[offset] = (byte)value;
            buffer[offset + 1] = (byte)(value >> 8);
            buffer[offset + 2] = (byte)(value >> 16);
            buffer[offset + 3] = (byte)(value >> 24);
        }
    }
}
