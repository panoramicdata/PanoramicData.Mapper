namespace PanoramicData.Mapper.Test;

public class MemberListTests
{
    private sealed class Source
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
    }

    private sealed class Destination
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private sealed class SmallSource
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private sealed class DestinationWithExtra
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Extra { get; set; } = string.Empty;
    }

    private sealed class MemberListSourceAllMappedProfile : Profile
    {
        public MemberListSourceAllMappedProfile()
        {
            // SmallSource has Id and Name, both exist on DestinationWithExtra
            // Extra on destination is unmatched but MemberList.Source only validates source members
            CreateMap<SmallSource, DestinationWithExtra>(MemberList.Source);
        }
    }

    private sealed class MemberListSourceUnmappedProfile : Profile
    {
        public MemberListSourceUnmappedProfile()
        {
            CreateMap<Source, Destination>(MemberList.Source);
        }
    }

    private sealed class MemberListSourceDoNotValidateProfile : Profile
    {
        public MemberListSourceDoNotValidateProfile()
        {
            CreateMap<Source, Destination>(MemberList.Source)
                .ForSourceMember(source => source.ApiKey, opt => opt.DoNotValidate());
        }
    }

    private sealed class MemberListNoneProfile : Profile
    {
        public MemberListNoneProfile()
        {
            CreateMap<Source, Destination>(MemberList.None);
        }
    }

    [Fact]
    public void MemberListSource_AllSourceMembersMapped_DoesNotThrow()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<MemberListSourceAllMappedProfile>());

        // SmallSource.Id and SmallSource.Name both map to DestinationWithExtra
        // DestinationWithExtra.Extra is unmatched but irrelevant in MemberList.Source mode
        var act = () => config.AssertConfigurationIsValid();

        act.Should().NotThrow();
    }

    [Fact]
    public void MemberListSource_UnmappedSourceMember_Throws()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<MemberListSourceUnmappedProfile>());

        // ApiKey exists on Source but not on Destination → unmapped source member
        var act = () => config.AssertConfigurationIsValid();

        act.Should().Throw<AutoMapperConfigurationException>();
    }

    [Fact]
    public void MemberListSource_ForSourceMemberDoNotValidate_ExcludesMember()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<MemberListSourceDoNotValidateProfile>());

        var act = () => config.AssertConfigurationIsValid();

        act.Should().NotThrow();
    }

    [Fact]
    public void MemberListNone_SkipsValidationEntirely()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<MemberListNoneProfile>());

        var act = () => config.AssertConfigurationIsValid();

        act.Should().NotThrow();
    }

    [Fact]
    public void MemberListSource_MappingStillWorks()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<MemberListSourceDoNotValidateProfile>());

        var mapper = config.CreateMapper();
        var source = new Source { Id = 42, Name = "Test", ApiKey = "secret" };
        var result = mapper.Map<Destination>(source);

        result.Id.Should().Be(42);
        result.Name.Should().Be("Test");
    }
}
