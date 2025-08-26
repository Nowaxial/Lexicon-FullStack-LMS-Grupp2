using AutoMapper;
using Domain.Models.Entities;
using LMS.Shared.DTOs.AuthDtos;
using LMS.Shared.DTOs.EntitiesDtos;

namespace LMS.Infractructure.Data;

public class MapperProfile : Profile
{
    public MapperProfile()
    {
        CreateMap<UserRegistrationDto, ApplicationUser>();
        
        // Map Entity → DTO
        CreateMap<Course, CourseDto>();
        CreateMap<Module, ModuleDto>()
          .ForMember(dest => dest.ActivitiesCount, opt => opt.MapFrom(src => src.Activities.Count))
          .ForMember(dest => dest.DocumentsCount, opt => opt.MapFrom(src => src.Documents.Count));

        // Map DTO → Entity
        CreateMap<CreateCourseDto, Course>();
        CreateMap<UpdateCourseDto, Course>();

        CreateMap<ModuleCreateDto, Module>();
        CreateMap<ModuleUpdateDto, Module>();
    }


}
