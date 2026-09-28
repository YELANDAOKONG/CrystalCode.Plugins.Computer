namespace CrystalCode.Computer.VirtualBox;

internal sealed class ProcessStartException : InvalidOperationException
{
    public ProcessStartException(Exception inner)
        : base("The process could not be started.", inner)
    {
    }
}
