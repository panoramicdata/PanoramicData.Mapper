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

    private class IncludeProfile : Profile
    {
        public IncludeProfile()
        {
            CreateMap<AnimalSource, AnimalDest>()
                .Include<DogSource, DogDest>();

            CreateMap<DogSource, DogDest>();
        }
    }

    private class IncludeBaseProfile : Profile
    {
        public IncludeBaseProfile()
        {
            CreateMap<AnimalSource, AnimalDest>();

            CreateMap<CatSource, CatDest>()
                .IncludeBase<AnimalSource, AnimalDest>();
        }
    }

    private class IncludeAllDerivedProfile : Profile
    {
        public IncludeAllDerivedProfile()
        {
            CreateMap<AnimalSource, AnimalDest>()
                .IncludeAllDerived();
        }
    }

    [Fact]
    public void IncludeAndIncludeBase_Together_BothDerivedTypesInheritBaseConfig()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile(new IncludeAndIncludeBaseProfile()));
        var mapper = config.CreateMapper();

        var dog = new DogSource { Name = "Rex", Legs = 4, Breed = "Labrador" };
        var dogDest = mapper.Map<DogDest>(dog);
        dogDest.Name.Should().Be("Rex");
        dogDest.Legs.Should().Be(4);
        dogDest.Breed.Should().Be("Labrador");

        var cat = new CatSource { Name = "Whiskers", Legs = 4, IsIndoor = true };
        var catDest = mapper.Map<CatDest>(cat);
        catDest.Name.Should().Be("Whiskers");
        catDest.Legs.Should().Be(4);
        catDest.IsIndoor.Should().BeTrue();
    }

    [Fact]
    public void IncludeAndIncludeBase_Together_BaseAfterMapPropagates()
    {
        var afterMapCalled = false;
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile(new IncludeBaseWithAfterMapProfile(() => afterMapCalled = true)));
        var mapper = config.CreateMapper();

        var dog = new DogSource { Name = "Buddy", Legs = 4, Breed = "Poodle" };
        var dest = mapper.Map<DogDest>(dog);

        dest.Name.Should().Be("Buddy");
        afterMapCalled.Should().BeTrue();
    }

    private class IncludeAndIncludeBaseProfile : Profile
    {
        public IncludeAndIncludeBaseProfile()
        {
            // Include: base declares DogSource->DogDest as derived
            CreateMap<AnimalSource, AnimalDest>()
                .Include<DogSource, DogDest>();

            CreateMap<DogSource, DogDest>();

            // IncludeBase: CatSource->CatDest declares base
            CreateMap<CatSource, CatDest>()
                .IncludeBase<AnimalSource, AnimalDest>();
        }
    }

    private class IncludeBaseWithAfterMapProfile : Profile
    {
        private readonly Action _onAfterMap;

        public IncludeBaseWithAfterMapProfile(Action onAfterMap)
        {
            _onAfterMap = onAfterMap;

            CreateMap<AnimalSource, AnimalDest>()
                .AfterMap((_, _) => _onAfterMap())
                .Include<DogSource, DogDest>();

            CreateMap<DogSource, DogDest>()
                .IncludeBase<AnimalSource, AnimalDest>();
        }
    }
}
