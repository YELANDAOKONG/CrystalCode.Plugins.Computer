namespace CrystalCode.Computer;

internal sealed record GuestAdditionsReport(GuestAdditionsState State, string? Version)
{
    public string Describe()
    {
        return State switch
        {
            GuestAdditionsState.Unavailable =>
                "Guest Additions status is reported while the VM is running.",
            GuestAdditionsState.NotLoaded =>
                Version is null
                    ? "Guest Additions are not loaded."
                    : $"Guest Additions {Version} are not loaded.",
            GuestAdditionsState.DriversLoaded =>
                Version is null
                    ? "Guest Additions drivers are loaded, but the guest service is not active."
                    : $"Guest Additions {Version} drivers are loaded, but the guest service is not active.",
            GuestAdditionsState.Active =>
                Version is null
                    ? "Guest Additions are active."
                    : $"Guest Additions {Version} are active.",
            _ => throw new InvalidOperationException("Guest Additions state is unknown.")
        };
    }
}
