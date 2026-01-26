using System.Security.Cryptography;
using Microsoft.Maui.Storage;

namespace MyMauiApp.Services;

public class PinService : IPinService
{
    private const string KeyEnabled = "pin_enabled";
    private const string KeySalt = "pin_salt";
    private const string KeyHash = "pin_hash";

    // PBKDF2 settings
    private const int Iterations = 100_000;
    private const int SaltSize = 16;
    private const int HashSize = 32;

    public async Task<bool> IsPinEnabledAsync()
    {
        var v = await SafeGetAsync(KeyEnabled);
        return string.Equals(v, "1", StringComparison.Ordinal);
    }

    public async Task<bool> HasPinAsync()
    {
        var hash = await SafeGetAsync(KeyHash);
        var salt = await SafeGetAsync(KeySalt);
        return !string.IsNullOrWhiteSpace(hash) && !string.IsNullOrWhiteSpace(salt);
    }

    public async Task<bool> VerifyPinAsync(string pin)
    {
        if (!IsValidPin(pin)) return false;

        var enabled = await IsPinEnabledAsync();
        if (!enabled) return true;

        var saltB64 = await SafeGetAsync(KeySalt);
        var hashB64 = await SafeGetAsync(KeyHash);

        if (string.IsNullOrWhiteSpace(saltB64) || string.IsNullOrWhiteSpace(hashB64))
            return false;

        var salt = Convert.FromBase64String(saltB64);
        var expected = Convert.FromBase64String(hashB64);
        var actual = HashPin(pin, salt);

        return CryptographicOperations.FixedTimeEquals(expected, actual);
    }

    public async Task EnablePinAsync(string newPin)
    {
        if (!IsValidPin(newPin))
            throw new ArgumentException("PIN must be exactly 4 digits.");

        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = HashPin(newPin, salt);

        await SafeSetAsync(KeySalt, Convert.ToBase64String(salt));
        await SafeSetAsync(KeyHash, Convert.ToBase64String(hash));
        await SafeSetAsync(KeyEnabled, "1");
    }

    public async Task ChangePinAsync(string currentPin, string newPin)
    {
        if (!IsValidPin(newPin))
            throw new ArgumentException("New PIN must be exactly 4 digits.");

        var ok = await VerifyPinAsync(currentPin);
        if (!ok)
            throw new InvalidOperationException("Current PIN is incorrect.");

        await EnablePinAsync(newPin); // re-salt + re-hash
    }

    public async Task DisablePinAsync(string currentPin)
    {
        var ok = await VerifyPinAsync(currentPin);
        if (!ok)
            throw new InvalidOperationException("PIN is incorrect.");

        await SafeSetAsync(KeyEnabled, "0");
    }

    public Task<string> GeneratePinAsync()
    {
        var n = RandomNumberGenerator.GetInt32(0, 10000);
        return Task.FromResult(n.ToString("D4"));
    }

    // ----------------------------
    // Helpers
    // ----------------------------
    private static bool IsValidPin(string pin)
        => pin.Length == 4 && pin.All(char.IsDigit);

    private static byte[] HashPin(string pin, byte[] salt)
    {
        using var pbkdf2 = new Rfc2898DeriveBytes(
            password: pin,
            salt: salt,
            iterations: Iterations,
            hashAlgorithm: HashAlgorithmName.SHA256);

        return pbkdf2.GetBytes(HashSize);
    }

    /// <summary>
    /// SecureStorage can throw on some Windows setups (unpackaged).
    /// This safely falls back to Preferences.
    /// </summary>
    private static async Task<string?> SafeGetAsync(string key)
    {
        try
        {
            return await SecureStorage.GetAsync(key);
        }
        catch
        {
            // fallback
            return Preferences.Default.Get<string?>(key, null);
        }
    }

    private static async Task SafeSetAsync(string key, string value)
    {
        try
        {
            await SecureStorage.SetAsync(key, value);
        }
        catch
        {
            // fallback
            Preferences.Default.Set(key, value);
        }
    }
}
