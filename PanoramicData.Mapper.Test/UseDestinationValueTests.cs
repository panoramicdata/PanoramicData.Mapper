using PanoramicData.Mapper.Test.Models;

namespace PanoramicData.Mapper.Test;

public class UseDestinationValueTests
{
    [Fact]
    public void UseDestinationValue_PreservesExistingDestinationValue()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile(new UseDestProfile()));
        var mapper = config.CreateMapper();

        var source = new UseDestValSource { Name = "Updated" };
        var existing = new UseDestValDest { Name = "Old", Existing = "keep-me" };

        var dest = mapper.Map(source, existing);

        dest.Name.Should().Be("Updated");
        dest.Existing.Should().Be("keep-me"); // Preserved because UseDestinationValue + Ignore
    }

    private class UseDestProfile : Profile
    {
        public UseDestProfile()
        {
            CreateMap<UseDestValSource, UseDestValDest>()
                .ForMember(d => d.Existing, opt => opt.Ignore());
        }
    }
}
