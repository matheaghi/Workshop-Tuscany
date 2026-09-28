namespace TronderLeikan.Domain.Tournaments;

// Poengene én person fikk i ett spill, fordelt på hva de ble gitt for
public sealed record GamePoints(int Participation, int Placement, int Organizing, int Spectating)
{
    public int Total => Participation + Placement + Organizing + Spectating;
}
