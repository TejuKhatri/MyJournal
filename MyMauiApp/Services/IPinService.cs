namespace MyMauiApp.Services;

public interface IPinService
{
    Task<bool> IsPinEnabledAsync();
    Task<bool> HasPinAsync();

    Task<bool> VerifyPinAsync(string pin);

    Task EnablePinAsync(string newPin);
    Task ChangePinAsync(string currentPin, string newPin);
    Task DisablePinAsync(string currentPin);

    Task<string> GeneratePinAsync();
}
