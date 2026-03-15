using PanoramicData.Mapper.Test.Models;
using PanoramicData.Mapper.Test.Profiles;

namespace PanoramicData.Mapper.Test;

public class IgnoreTests
{
	[Fact]
	public void ForMember_Ignore_SkipsProperty()
	{
		var config = new MapperConfiguration(cfg => cfg.AddProfile<IgnoreProfile>());
		var mapper = config.CreateMapper();

		var source = new SimpleSource { Id = 1, Name = "Test" };
		var dest = mapper.Map<DestinationWithIgnoredProps>(source);

		dest.Id.Should().Be(1);
		dest.Name.Should().Be("Test");
		dest.Secret.Should().Be("original"); // Not overwritten
		dest.Timestamp.Should().Be(default(DateTime)); // Not mapped
	}

	[Fact]
	public void IgnoreAttribute_SkipsProperty()
	{
		var config = new MapperConfiguration(cfg =>
			cfg.AddProfile(new TestProfile()));
		var mapper = config.CreateMapper();

		var source = new SimpleSource { Id = 1, Name = "Test" };
		var dest = mapper.Map<DestinationWithIgnoreAttribute>(source);

		dest.Id.Should().Be(1);
		dest.Name.Should().Be("Test");
		dest.Secret.Should().Be("original");
	}

	private class TestProfile : Profile
	{
		public TestProfile()
		{
			CreateMap<SimpleSource, DestinationWithIgnoreAttribute>();
		}
	}
}