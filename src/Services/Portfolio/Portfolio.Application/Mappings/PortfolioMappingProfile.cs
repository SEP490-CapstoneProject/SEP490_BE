using AutoMapper;
using Portfolio.Application.DTOs;
using Portfolio.Domain.Entities;

namespace Portfolio.Application.Mappings;

public class PortfolioMappingProfile : Profile
{
    public PortfolioMappingProfile()
    {
        // Portfolio
        CreateMap<Domain.Entities.Portfolio, PortfolioDto>()
            .ForMember(d => d.PortfolioId, o => o.MapFrom(s => s.Id))
            .ForMember(d => d.PortfolioName, o => o.MapFrom(s => s.Name))
            .ForMember(d => d.Blocks, o => o.Ignore()); // mapped manually

        // Data DTOs
        CreateMap<Intro, IntroDataDto>();
        CreateMap<Skill, SkillDataDto>();
        CreateMap<Education, EducationDataDto>();
        CreateMap<Diploma, DiplomaDataDto>();
        CreateMap<Experience, ExperienceDataDto>();
        CreateMap<Project, ProjectDataDto>()
            .ForMember(d => d.Links, o => o.MapFrom(s => s.Links));
        CreateMap<ProjectLink, ProjectLinkDto>();
        CreateMap<Award, AwardDataDto>();
        CreateMap<Activities, ActivitiesDataDto>();
        CreateMap<OtherInfo, OtherInfoDataDto>();
        CreateMap<Reference, ReferenceDataDto>();
    }
}
