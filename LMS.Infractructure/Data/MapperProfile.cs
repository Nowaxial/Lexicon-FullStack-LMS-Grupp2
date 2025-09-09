using AutoMapper;
using Domain.Models.Entities;
using LMS.Shared.DTOs.AuthDtos;
using LMS.Shared.DTOs.EntitiesDtos;
using LMS.Shared.DTOs.EntitiesDtos.ModulesDtos;
using LMS.Shared.DTOs.EntitiesDtos.ProjActivity;
using LMS.Shared.DTOs.EntitiesDtos.ProjDocumentDtos;

namespace LMS.Infractructure.Data;

public class MapperProfile : Profile
{
    public MapperProfile()
    {
        CreateMap<UserRegistrationDto, ApplicationUser>();
        
        // Map Entity → DTO
        CreateMap<Course, CourseDto>()
            .ForMember(d => d.Modules,
                opt => opt.MapFrom(s => s.Modules.OrderBy(m => m.Starts)));

        CreateMap<Module, ModuleDto>()
          .ForMember(d => d.Activities,
          opt => opt.MapFrom(s => s.Activities.OrderBy(m => m.Starts)));
          
          //.ForMember(dest => dest.ActivitiesCount, opt => opt.MapFrom(src => src.Activities.Count))
          //.ForMember(dest => dest.DocumentsCount, opt => opt.MapFrom(src => src.Documents.Count));

        // Map DTO → Entity
        CreateMap<CreateCourseDto, Course>();
        CreateMap<UpdateCourseDto, Course>();

        CreateMap<ModuleCreateDto, Module>();
        CreateMap<ModuleUpdateDto, Module>();

        CreateMap<ProjActivity, ProjActivityDto>();
        CreateMap<CreateProjActivityDto, ProjActivity>();
        CreateMap<UpdateProjActivityDto, ProjActivity>();

        CreateMap<ProjDocument, ProjDocumentDto>();
        CreateMap<UploadProjDocumentDto, ProjDocument>();
        CreateMap<UpdateProjDocumentDto, ProjDocument>();
    }

}
