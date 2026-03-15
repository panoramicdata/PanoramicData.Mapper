using PanoramicData.Mapper.Test.Models;

namespace PanoramicData.Mapper.Test;

public class MaxDepthTests
{
    [Fact]
    public void MaxDepth_TruncatesAtSpecifiedDepth()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile(new MaxDepthProfile()));
        var mapper = config.CreateMapper();

        var source = new TreeNodeSource
        {
            Name = "Level1",
            Child = new TreeNodeSource
            {
                Name = "Level2",
                Child = new TreeNodeSource
                {
                    Name = "Level3",
                    Child = new TreeNodeSource
                    {
                        Name = "Level4"
                    }
                }
            }
        };

        var dest = mapper.Map<TreeNodeDest>(source);

        dest.Name.Should().Be("Level1");
        dest.Child.Should().NotBeNull();
        dest.Child!.Name.Should().Be("Level2");
        dest.Child.Child.Should().NotBeNull();
        dest.Child.Child!.Name.Should().Be("Level3");
        // Level4 should be truncated (depth=3 means 3 levels deep)
        dest.Child.Child.Child.Should().NotBeNull();
        dest.Child.Child.Child!.Name.Should().Be(string.Empty); // Default instance, mapping not performed
    }

    [Fact]
    public void MaxDepth_WithinLimit_MapsFullDepth()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile(new MaxDepthProfile()));
        var mapper = config.CreateMapper();

        var source = new TreeNodeSource
        {
            Name = "Root",
            Child = new TreeNodeSource
            {
                Name = "Child"
            }
        };

        var dest = mapper.Map<TreeNodeDest>(source);

        dest.Name.Should().Be("Root");
        dest.Child.Should().NotBeNull();
        dest.Child!.Name.Should().Be("Child");
        dest.Child.Child.Should().BeNull();
    }

    private class MaxDepthProfile : Profile
    {
        public MaxDepthProfile()
        {
            CreateMap<TreeNodeSource, TreeNodeDest>()
                .MaxDepth(3);
        }
    }
}
