using PanoramicData.Mapper.Test.Models;
using PanoramicData.Mapper.Test.Profiles;
using System.Diagnostics;

namespace PanoramicData.Mapper.Test;

public class PerformanceTests
{
    [Fact]
    public void ConfigurationCreation_CompletesWithinReasonableTime()
    {
        var sw = Stopwatch.StartNew();

        for (var i = 0; i < 1000; i++)
        {
            _ = new MapperConfiguration(cfg => cfg.AddProfile<SimpleProfile>());
        }

        sw.Stop();

        // 1000 configuration creations should complete well under 1 second
        sw.ElapsedMilliseconds.Should().BeLessThan(1000);
    }

    [Fact]
    public void SingleMap_CompletesWithinReasonableTime()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<SimpleProfile>());
        var mapper = config.CreateMapper();
        var source = new SimpleSource
        {
            Id = 1,
            Name = "Test",
            Description = "Desc",
            CreatedDate = DateTime.UtcNow,
            Amount = 99.95m
        };

        // Warm up (triggers compiled mapper creation)
        mapper.Map<SimpleDestination>(source);

        var sw = Stopwatch.StartNew();

        for (var i = 0; i < 10_000; i++)
        {
            mapper.Map<SimpleDestination>(source);
        }

        sw.Stop();

        // 10k maps should complete well under 1 second with compiled mapper
        sw.ElapsedMilliseconds.Should().BeLessThan(1000);
    }

    [Fact]
    public void BulkMapping_10kObjects_CompletesWithinReasonableTime()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<SimpleProfile>());
        var mapper = config.CreateMapper();

        var sources = Enumerable.Range(0, 10_000)
            .Select(i => new SimpleSource
            {
                Id = i,
                Name = $"Item {i}",
                Description = $"Description for item {i}",
                CreatedDate = DateTime.UtcNow.AddDays(-i),
                Amount = i * 1.5m
            })
            .ToList();

        // Warm up
        mapper.Map<SimpleDestination>(sources[0]);

        var sw = Stopwatch.StartNew();

        foreach (var source in sources)
        {
            mapper.Map<SimpleDestination>(source);
        }

        sw.Stop();

        // Bulk mapping 10k objects should complete well under 2 seconds
        sw.ElapsedMilliseconds.Should().BeLessThan(2000);
    }

    [Fact]
    public void CompiledMapperCaching_SecondCallIsFaster()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<SimpleProfile>());
        var mapper = config.CreateMapper();
        var source = new SimpleSource
        {
            Id = 1,
            Name = "Test",
            Description = "Desc",
            CreatedDate = DateTime.UtcNow,
            Amount = 10m
        };

        // First call triggers compilation
        var sw1 = Stopwatch.StartNew();
        for (var i = 0; i < 100; i++)
        {
            // Create fresh config each time to force re-compilation
            var freshConfig = new MapperConfiguration(cfg => cfg.AddProfile<SimpleProfile>());
            var freshMapper = freshConfig.CreateMapper();
            freshMapper.Map<SimpleDestination>(source);
        }

        sw1.Stop();

        // Second run reuses compiled mappers (single config)
        var sw2 = Stopwatch.StartNew();
        for (var i = 0; i < 100; i++)
        {
            mapper.Map<SimpleDestination>(source);
        }

        sw2.Stop();

        // Cached mapper should be significantly faster than repeated fresh compilations
        sw2.ElapsedTicks.Should().BeLessThan(sw1.ElapsedTicks);
    }
}
