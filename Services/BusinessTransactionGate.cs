namespace ManaChaiLeasing.Services;

internal static class BusinessTransactionGate
{
    internal static object SyncRoot { get; } = new();
}
