using Microsoft.EntityFrameworkCore;
using PanoramicData.Mapper.Test.Models;

namespace PanoramicData.Mapper.Test;

public class ProjectToTests
{
	[Fact]
	public void ProjectTo_ConventionMapping_ProjectsCorrectly()
	{
		var config = new MapperConfiguration(cfg =>
			cfg.AddProfile(new SimpleProjectProfile()));

		using var context = CreateContext();
		SeedData(context);

		var projected = context.Sources
			.ProjectTo<SimpleDestination>(config)
			.ToList();

		projected.Should().HaveCount(2);
		projected[0].Id.Should().Be(1);
		projected[0].Name.Should().Be("First");
		projected[1].Id.Should().Be(2);
		projected[1].Name.Should().Be("Second");
	}

	[Fact]
	public void ProjectTo_WithMapFrom_ProjectsCustomExpression()
	{
		var config = new MapperConfiguration(cfg =>
			cfg.AddProfile(new PersonProjectProfile()));

		using var context = CreateContext();
		context.Persons.Add(new PersonSource { FirstName = "John", LastName = "Doe", Age = 30 });
		context.Persons.Add(new PersonSource { FirstName = "Jane", LastName = "Smith", Age = 25 });
		context.SaveChanges();

		var projected = context.Persons
			.ProjectTo<PersonDest>(config)
			.ToList();

		projected.Should().HaveCount(2);
		projected[0].FullName.Should().Be("John Doe");
		projected[0].Age.Should().Be(30);
		projected[1].FullName.Should().Be("Jane Smith");
		projected[1].Age.Should().Be(25);
	}

	[Fact]
	public void ProjectTo_WithIgnore_SkipsIgnoredProperties()
	{
		var config = new MapperConfiguration(cfg =>
			cfg.AddProfile(new IgnoreProjectProfile()));

		using var context = CreateContext();
		SeedData(context);

		var projected = context.Sources
			.ProjectTo<DestinationWithIgnoredProps>(config)
			.ToList();

		projected.Should().HaveCount(2);
		projected[0].Id.Should().Be(1);
		projected[0].Name.Should().Be("First");
		projected[0].Secret.Should().Be("original"); // Ignored = don't map, so class initializer value is preserved
	}

	private static TestDbContext CreateContext()
	{
		var options = new DbContextOptionsBuilder<TestDbContext>()
			.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
			.Options;
		return new TestDbContext(options);
	}

	private static void SeedData(TestDbContext context)
	{
		context.Sources.Add(new SimpleSource
		{
			Id = 1,
			Name = "First",
			Description = "Desc1",
			CreatedDate = new DateTime(2026, 1, 1),
			Amount = 10m
		});
		context.Sources.Add(new SimpleSource
		{
			Id = 2,
			Name = "Second",
			Description = "Desc2",
			CreatedDate = new DateTime(2026, 2, 1),
			Amount = 20m
		});
		context.SaveChanges();
	}

	private class SimpleProjectProfile : Profile
	{
		public SimpleProjectProfile()
		{
			CreateMap<SimpleSource, SimpleDestination>();
		}
	}

	private class PersonProjectProfile : Profile
	{
		public PersonProjectProfile()
		{
			CreateMap<PersonSource, PersonDest>()
				.ForMember(d => d.FullName, opt => opt.MapFrom(s => s.FirstName + " " + s.LastName));
		}
	}

	private class IgnoreProjectProfile : Profile
	{
		public IgnoreProjectProfile()
		{
			CreateMap<SimpleSource, DestinationWithIgnoredProps>()
				.ForMember(d => d.Secret, opt => opt.Ignore())
				.ForMember(d => d.Timestamp, opt => opt.Ignore());
		}
	}
}

public class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
{
	public DbSet<SimpleSource> Sources { get; set; } = null!;
	public DbSet<PersonSource> Persons { get; set; } = null!;

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.Entity<SimpleSource>().HasKey(e => e.Id);
		modelBuilder.Entity<PersonSource>().HasKey(e => e.FirstName);
	}
}