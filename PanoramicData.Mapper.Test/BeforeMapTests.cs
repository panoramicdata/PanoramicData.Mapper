using PanoramicData.Mapper.Test.Models;

namespace PanoramicData.Mapper.Test;

public class BeforeMapTests
{
    [Fact]
    public void BeforeMap_Lambda_ExecutesBeforeMapping()
    {
        var mapper = MapperFactory.Create<BeforeMapLambdaProfile>();

        var source = new BeforeMapSource { Id = 1, Name = "Test" };
        var dest = mapper.Map<BeforeMapDest>(source);

        AssertMapped(dest, 1, "Test", "pre-processed");
    }

    [Fact]
    public void BeforeMap_MappingAction_ExecutesBeforeMapping()
    {
        var mapper = MapperFactory.Create<BeforeMapActionProfile>();

        var source = new BeforeMapSource { Id = 5, Name = "ActionTest" };
        var dest = mapper.Map<BeforeMapDest>(source);

        AssertMapped(dest, 5, "ActionTest", "action-tag");
    }

    [Fact]
    public void BeforeMap_Lambda_MapToExisting_ExecutesBeforeMapping()
    {
        var mapper = MapperFactory.Create<BeforeMapLambdaProfile>();

        var source = new BeforeMapSource { Id = 1, Name = "Test" };
        var dest = new BeforeMapDest { Tag = "original" };

        mapper.Map(source, dest);

        AssertMapped(dest, 1, "Test", "pre-processed");
    }

    private static void AssertMapped(BeforeMapDest dest, int id, string name, string tag)
    {
        dest.Id.Should().Be(id);
        dest.Name.Should().Be(name);
        dest.Tag.Should().Be(tag);
    }

    private class BeforeMapLambdaProfile : Profile
    {
        public BeforeMapLambdaProfile()
        {
            CreateMap<BeforeMapSource, BeforeMapDest>()
                .BeforeMap((_, dest) => dest.Tag = "pre-processed");
        }
    }

    private class BeforeMapAction : IMappingAction<BeforeMapSource, BeforeMapDest>
    {
        public void Process(BeforeMapSource source, BeforeMapDest destination, ResolutionContext context)
        {
            source.Should().NotBeNull();
            context.Should().NotBeNull();
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
