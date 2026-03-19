using PanoramicData.Mapper.Test.Models;

namespace PanoramicData.Mapper.Test;

public class InheritanceTests
{
    [Fact]
    public void Include_DerivedTypeMaps_InheritBaseConfig()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile(new IncludeProfile()));
        var mapper = config.CreateMapper();

        var dog = new DogSource { Name = "Rex", Legs = 4, Breed = "Labrador" };
        var dest = mapper.Map<DogDest>(dog);

        dest.Name.Should().Be("Rex");
        dest.Legs.Should().Be(4);
        dest.Breed.Should().Be("Labrador");
    }

    [Fact]
    public void IncludeBase_DerivedInheritsBaseConfig()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile(new IncludeBaseProfile()));
        var mapper = config.CreateMapper();

        var cat = new CatSource { Name = "Whiskers", Legs = 4, IsIndoor = true };
        var dest = mapper.Map<CatDest>(cat);

        dest.Name.Should().Be("Whiskers");
        dest.Legs.Should().Be(4);
        dest.IsIndoor.Should().BeTrue();
    }

    [Fact]
    public void IncludeAllDerived_AutomaticallyMapsDerivedTypes()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile(new IncludeAllDerivedProfile()));
        var mapper = config.CreateMapper();

        var dog = new DogSource { Name = "Buddy", Legs = 4, Breed = "Poodle" };
        var dest = mapper.Map<DogSource, DogDest>(dog);

        dest.Name.Should().Be("Buddy");
        dest.Legs.Should().Be(4);
        dest.Breed.Should().Be("Poodle");
    }

    private sealed class IncludeProfile : Profile
    {
        public IncludeProfile()
        {
            CreateMap<AnimalSource, AnimalDest>()
                .Include<DogSource, DogDest>();

            CreateMap<DogSource, DogDest>();
        }
    }

    private sealed class IncludeBaseProfile : Profile
    {
        public IncludeBaseProfile()
        {
            CreateMap<AnimalSource, AnimalDest>();

            CreateMap<CatSource, CatDest>()
                .IncludeBase<AnimalSource, AnimalDest>();
        }
    }

    private sealed class IncludeAllDerivedProfile : Profile
    {
        public IncludeAllDerivedProfile()
        {
            CreateMap<AnimalSource, AnimalDest>()
                .IncludeAllDerived();
        }
    }
}
