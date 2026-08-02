namespace DentalLocal.Domain.Enums;

public enum ToothStateType : short
{
    Healthy = 0,
    Caries = 1,
    Filling = 2,
    Extracted = 3,
    Crown = 4,
    Implant = 5,
    Veneer = 6,
    RootCanalTreated = 7,
    Cracked = 8,
    Destroyed = 9,
    Pulpitis = 10,
    Periodontitis = 11,
    Bridge = 12,
    Missing = 13,
    Deciduous = 14,

    /// <summary>Молочный зуб выпал/сменился постоянным (см. промпт 31 аудита — смена прикуса).</summary>
    Exfoliated = 15,
}
