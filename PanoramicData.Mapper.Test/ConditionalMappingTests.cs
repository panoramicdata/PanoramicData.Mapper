using PanoramicData.Mapper.Test.Models;

namespace PanoramicData.Mapper.Test;

public class ConditionalMappingTests
{
    [Fact]
    public void Condition_WhenTrue_MapsValue()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile(new ConditionProfile()));
        var mapper = config.CreateMapper();

        var source = new ConditionalSource { Id = 1, Name = "Hello", Age = 25, IsActive = true };
        var dest = mapper.Map<ConditionalDest>(source);

        dest.Name.Should().Be("Hello");
    }

    [Fact]
    public void Condition_WhenFalse_SkipsMapping()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile(new ConditionProfile()));
        var mapper = config.CreateMapper();

        var source = new ConditionalSource { Id = 1, Name = "Hello", Age = 25, IsActive = false };
        var dest = mapper.Map<ConditionalDest>(source);

        dest.Name.Should().Be("default"); // Not mapped because IsActive is false
    }

    [Fact]
    public void PreCondition_WhenFalse_SkipsMember()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile(new PreConditionProfile()));
        var mapper = config.CreateMapper();

        var source = new ConditionalSource { Id = 1, Name = "Hello", Age = -1, IsActive = true };
        var dest = mapper.Map<ConditionalDest>(source);

        dest.Age.Should().Be(0); // PreCondition skips because Age < 0
    }

    [Fact]
    public void PreCondition_WhenTrue_MapsValue()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile(new PreConditionProfile()));
        var mapper = config.CreateMapper();

        var source = new ConditionalSource { Id = 1, Name = "Hello", Age = 30, IsActive = true };
        var dest = mapper.Map<ConditionalDest>(source);

        dest.Age.Should().Be(30);
    }

    private class ConditionProfile : Profile
    {
        public ConditionProfile()
        {
            CreateMap<ConditionalSource, ConditionalDest>()
                .ForMember(d => d.Name, opt =>
                {
                    opt.MapFrom(s => s.Name!);
                    opt.Condition((src, _, _) => src.IsActive);
                });
        }
    }

    private class PreConditionProfile : Profile
    {
        public PreConditionProfile()
        {
            CreateMap<ConditionalSource, ConditionalDest>()
                .ForMember(d => d.Age, opt =>
                {
                    opt.MapFrom(s => s.Age);
                    opt.PreCondition(s => s.Age >= 0);
                });
        }
    }
}
