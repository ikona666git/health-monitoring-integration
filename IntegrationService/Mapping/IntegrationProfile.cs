using AutoMapper;
using IntegrationService.Models;

namespace IntegrationService.Mapping;

public class IntegrationProfile : Profile
{
    public IntegrationProfile()
    {
        CreateMap<Measurement, AlertRequest>()
            .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserId))
            .ForMember(dest => dest.MetricType, opt => opt.MapFrom(src => src.MetricType))
            .ForMember(dest => dest.Value, opt => opt.MapFrom(src => src.Value))
            .ForMember(dest => dest.AlertChannel, opt => opt.MapFrom(src => "console"));

        CreateMap<CheckResult, AlertRequest>()
            .ForMember(dest => dest.AlertChannel, opt => opt.MapFrom(src => "all"));
    }
}