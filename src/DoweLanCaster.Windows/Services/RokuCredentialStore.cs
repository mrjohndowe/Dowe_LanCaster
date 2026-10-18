using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace DoweLanCaster.Services;

public static class RokuCredentialStore
{
    private const uint UiForbidden = 0x1;

    public static byte[] GetOrCreateSalt(string? savedSalt)
    {
        try
        {
            var salt = Convert.FromBase64String(savedSalt ?? "");
            if (salt.Length == 32)
                return salt;
        }
        catch (FormatException) { }

        return RandomNumberGenerator.GetBytes(32);
    }

    public static string Protect(string password, byte[] salt)
    {
        var clearBytes = Encoding.UTF8.GetBytes(password);
        var input = CreateBlob(clearBytes);
        var entropy = CreateBlob(salt);
        try
        {
            if (!CryptProtectData(
                    ref input, "Dowe LanCaster Roku password", ref entropy,
                    IntPtr.Zero, IntPtr.Zero, UiForbidden, out var output))
                throw new CryptographicException(Marshal.GetLastWin32Error());

            try
            {
                var protectedBytes = new byte[output.Length];
                Marshal.Copy(output.Data, protectedBytes, 0, output.Length);
                return Convert.ToBase64String(protectedBytes);
            }
            finally { LocalFree(output.Data); }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(clearBytes);
            Marshal.FreeHGlobal(input.Data);
            Marshal.FreeHGlobal(entropy.Data);
        }
    }

    public static string? TryUnprotect(string? protectedValue, byte[] salt)
    {
        if (string.IsNullOrWhiteSpace(protectedValue))
            return null;

        byte[] encryptedBytes;
        try { encryptedBytes = Convert.FromBase64String(protectedValue); }
        catch (FormatException) { return null; }

        var input = CreateBlob(encryptedBytes);
        var entropy = CreateBlob(salt);
        try
        {
            if (!CryptUnprotectData(
                    ref input, IntPtr.Zero, ref entropy, IntPtr.Zero,
                    IntPtr.Zero, UiForbidden, out var output))
                return null;

            try
            {
                var clearBytes = new byte[output.Length];
                Marshal.Copy(output.Data, clearBytes, 0, output.Length);
                try { return Encoding.UTF8.GetString(clearBytes); }
                finally { CryptographicOperations.ZeroMemory(clearBytes); }
            }
            finally { LocalFree(output.Data); }
        }
        finally
        {
            Marshal.FreeHGlobal(input.Data);
            Marshal.FreeHGlobal(entropy.Data);
        }
    }

    private static DataBlob CreateBlob(byte[] bytes)
    {
        var pointer = Marshal.AllocHGlobal(bytes.Length);
        Marshal.Copy(bytes, 0, pointer, bytes.Length);
        return new DataBlob { Length = bytes.Length, Data = pointer };
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DataBlob
    {
        public int Length;
        public IntPtr Data;
    }

    [DllImport("crypt32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CryptProtectData(
        ref DataBlob input, string description, ref DataBlob optionalEntropy,
        IntPtr reserved, IntPtr prompt, uint flags, out DataBlob output);

    [DllImport("crypt32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CryptUnprotectData(
        ref DataBlob input, IntPtr description, ref DataBlob optionalEntropy,
        IntPtr reserved, IntPtr prompt, uint flags, out DataBlob output);

    [DllImport("kernel32.dll")]
    private static extern IntPtr LocalFree(IntPtr memory);
}
