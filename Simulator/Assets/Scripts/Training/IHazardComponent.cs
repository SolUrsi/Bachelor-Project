/// <summary>
/// Interface for alle hazard-komponenter.
/// Brukes for registrering, reset, og tracking.
/// </summary>
public interface IHazardComponent
{
    string HazardId { get; }
    void Reset();
}
