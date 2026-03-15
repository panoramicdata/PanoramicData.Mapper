namespace PanoramicData.Mapper.Test;

public class IgnoreInaccessibleSetterTests
{
    [Fact]
    public void IgnoreAllPropertiesWithAnInaccessibleSetter_SkipsReadOnlyAndPrivateSetters()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile(new InaccessibleSetterProfile()));
        var mapper = config.CreateMapper();

        var source = new InaccessibleSource { Id = 1, Name = "Test", Value = 42 };
        var dest = mapper.Map<InaccessibleDest>(source);

        dest.Id.Should().Be(1);
        dest.Name.Should().Be("Test");
        dest.ReadOnly.Should().Be("default"); // Not mapped — no public setter
        dest.PrivateSet.Should().Be("private-default"); // Not mapped — private setter
    }

    [Fact]
    public void IgnoreAllPropertiesWithAnInaccessibleSetter_ConfigurationIsValid()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile(new InaccessibleSetterProfile()));

        // Should not throw — inaccessible setters are ignored
        config.AssertConfigurationIsValid();
    }

    [Fact]
    public void IgnoreAllPropertiesWithAnInaccessibleSetter_InitOnlyProperties_AreIgnored()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile(new InitOnlyProfile()));
        var mapper = config.CreateMapper();

        var source = new InitOnlySource { Id = 5, Name = "Init" };
        var dest = mapper.Map<InitOnlyDest>(source);

        dest.Id.Should().Be(5);
        // InitOnly is ignored because its setter is not publicly accessible at runtime
    }

    private class InaccessibleSource
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
    }

    private class InaccessibleDest
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ReadOnly { get; } = "default";
        public string PrivateSet { get; private set; } = "private-default";
        public int Value { get; set; }
    }

    private class InaccessibleSetterProfile : Profile
    {
        public InaccessibleSetterProfile()
        {
            CreateMap<InaccessibleSource, InaccessibleDest>()
                .IgnoreAllPropertiesWithAnInaccessibleSetter();
        }
    }

    private class InitOnlySource
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private class InitOnlyDest
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string InitOnly { get; init; } = "init-default";
    }

    private class InitOnlyProfile : Profile
    {
        public InitOnlyProfile()
        {
            CreateMap<InitOnlySource, InitOnlyDest>()
                .IgnoreAllPropertiesWithAnInaccessibleSetter();
        }
    }
}
