using AutoMapper;
using Domain.Models.Entities;
using LMS.Shared.DTOs.AuthDtos;
using LMS.Shared.DTOs.EntitiesDtos;
using LMS.Shared.DTOs.EntitiesDtos.ProjActivity;

namespace LMS.Infractructure.Data;

public class MapperProfile : Profile
{
    public MapperProfile()
    {
        CreateMap<UserRegistrationDto, ApplicationUser>();
        
        // Map Entity → DTO
        CreateMap<Course, CourseDto>();

        // Map DTO → Entity
        CreateMap<CreateCourseDto, Course>();
        CreateMap<UpdateCourseDto, Course>();


        // Activity mappings
        CreateMap<ProjActivity, ProjActivityDto>();
        CreateMap<CreateProjActivityDto, ProjActivity>();
        CreateMap<UpdateProjActivityDto, ProjActivity>();
    }
}
