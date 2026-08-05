namespace WindowDock.Core.Models;

public enum ParkResultKind
{
    Success,
    InvalidWindow,
    AlreadyParked,
    AccessDenied,
    Failed,
    MinimizedFallback
}

public sealed class ParkResult
{
    public required ParkResultKind Kind { get; init; }
    public string? Message { get; init; }
    public ParkedWindowInfo? Window { get; init; }

    public bool IsSuccess =>
        Kind is ParkResultKind.Success or ParkResultKind.MinimizedFallback;
}
