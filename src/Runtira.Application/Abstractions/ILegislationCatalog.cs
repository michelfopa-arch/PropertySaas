namespace Runtira.Application.Abstractions
{
    public interface ILegislationCatalog
    {
        Runtira.Application.Features.RuntiraLegislationProfileDto? GetProfile(string countryCode, string regionCode);
    }
}
