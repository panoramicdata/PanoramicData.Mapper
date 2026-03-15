using PanoramicData.Mapper.Test.Models;

namespace PanoramicData.Mapper.Test;

public class BeforeMapTests
{
    [Fact]
    public void BeforeMap_Lambda_ExecutesBeforeMapping()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile(new BeforeMapLambdaProfile()));
        var mapper = config.CreateMapper();

        var source = new BeforeMapSource { Id = 1, Name = "Test" };
        var dest = mapper.Map<BeforeMapDest>(source);

        dest.Id.Should().Be(1);
        dest.Name.Should().Be("Test");
        dest.Tag.Should().Be("pre-processed");
    }

    [Fact]
    public void BeforeMap_MappingAction_ExecutesBeforeMapping()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile(new BeforeMapActionProfile()));
        var mapper = config.CreateMapper();

        var source = new BeforeMapSource { Id = 5, Name = "ActionTest" };
        var dest = mapper.Map<BeforeMapDest>(source);

        dest.Id.Should().Be(5);
        dest.Name.Should().Be("ActionTest");
        dest.Tag.Should().Be("action-tag");
    }

    private class BeforeMapLambdaProfile : Profile
    {
        public BeforeMapLambdaProfile()
        {
            CreateMap<BeforeMapSource, BeforeMapDest>()
                .BeforeMap((src, dest) => dest.Tag = "pre-processed");
        }
    }

    private class BeforeMapAction : IMappingAction<BeforeMapSource, BeforeMapDest>
    {
        public void Process(BeforeMapSource source, BeforeMapDest destination, ResolutionContext context)
        {
            destination.Tag = "action-tag";
        }
    }

    private class BeforeMapActionProfile : Profile
    {
        public BeforeMapActionProfile()
        {
            CreateMap<BeforeMapSource, BeforeMapDest>()
                .BeforeMap<BeforeMapAction>();
        }
    }
}
