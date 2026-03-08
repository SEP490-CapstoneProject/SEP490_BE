using AutoMapper;
using Portfolio.Application.DTOs;

namespace Portfolio.Application.Mappings;

public class PortfolioMappingProfile : Profile
{
    public PortfolioMappingProfile()
    {
        CreateMap<Domain.Entities.Portfolio, PortfolioDto>()
            .ForMember(d => d.PortfolioId, o => o.MapFrom(s => s.Id))
            .ForMember(d => d.PortfolioName, o => o.MapFrom(s => s.Name))
            .ForMember(d => d.Blocks, o => o.Ignore());
    }
}
