namespace MyMauiApp.Services
{
    public class AppLockState
    {
        public bool IsUnlocked { get; private set; }
        public event Action? OnChange;

        public void Unlock()
        {
            IsUnlocked = true;
            OnChange?.Invoke();
        }

        public void Lock()
        {
            IsUnlocked = false;
            OnChange?.Invoke();
        }
    }
}
