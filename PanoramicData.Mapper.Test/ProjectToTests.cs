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

	[Fact]
	public void ProjectTo_NullableDoubleToString_ProjectsWithoutError()
	{
		var config = new MapperConfiguration(cfg =>
			cfg.AddProfile(new NullableDoubleToStringProfile()));

		using var context = CreateContext();
		context.NullableDoubles.Add(new NullableDoubleEntity { Id = 1, Score = 3.14, Name = "Pi" });
		context.NullableDoubles.Add(new NullableDoubleEntity { Id = 2, Score = null, Name = "Null" });
		context.SaveChanges();

		var projected = context.NullableDoubles
			.ProjectTo<StringScoreDestination>(config)
			.ToList();

		projected.Should().HaveCount(2);
		projected[0].Name.Should().Be("Pi");
		projected[1].Name.Should().Be("Null");
	}

	private class NullableDoubleToStringProfile : Profile
	{
		public NullableDoubleToStringProfile() => CreateMap<NullableDoubleEntity, StringScoreDestination>();
	}

	[Fact]
	public void ProjectTo_NullableToNonNullable_WithValues_ProjectsCorrectly()
	{
		var config = new MapperConfiguration(cfg =>
			cfg.AddProfile(new NullablePortProfile()));

		using var context = CreateContext();
		context.NullablePorts.Add(new NullablePortEntity
		{
			Id = 1,
			TrafficSentKbps = 123.45,
			ClientCount = 10,
			IsOnline = true
		});
		context.SaveChanges();

		var projected = context.NullablePorts
			.ProjectTo<NonNullablePortDto>(config)
			.ToList();

		projected.Should().ContainSingle();
		projected[0].TrafficSentKbps.Should().Be(123.45);
		projected[0].ClientCount.Should().Be(10);
		projected[0].IsOnline.Should().BeTrue();
	}

	[Fact]
	public void ProjectTo_NullableToNonNullable_WithNulls_DefaultsToZeroOrFalse()
	{
		var config = new MapperConfiguration(cfg =>
			cfg.AddProfile(new NullablePortProfile()));

		using var context = CreateContext();
		context.NullablePorts.Add(new NullablePortEntity
		{
			Id = 1,
			TrafficSentKbps = null,
			ClientCount = null,
			IsOnline = null
		});
		context.SaveChanges();

		var projected = context.NullablePorts
			.ProjectTo<NonNullablePortDto>(config)
			.ToList();

		projected.Should().ContainSingle();
		projected[0].TrafficSentKbps.Should().Be(0.0);
		projected[0].ClientCount.Should().Be(0);
		projected[0].IsOnline.Should().BeFalse();
	}

	private class NullablePortProfile : Profile
	{
		public NullablePortProfile() => CreateMap<NullablePortEntity, NonNullablePortDto>();
	}

	[Fact]
	public void ProjectTo_NullSource_ThrowsArgumentNullException()
	{
		var config = new MapperConfiguration(cfg =>
			cfg.AddProfile(new SimpleProjectProfile()));

		IQueryable source = null!;
		var act = () => source.ProjectTo<SimpleDestination>(config);

		act.Should().Throw<ArgumentNullException>();
	}

	[Fact]
	public void ProjectTo_NullConfigurationProvider_ThrowsArgumentNullException()
	{
		using var context = CreateContext();

		var act = () => context.Sources.ProjectTo<SimpleDestination>(null!);

		act.Should().Throw<ArgumentNullException>();
	}

	[Fact]
	public void ProjectTo_NoTypeMap_MapsConventionPropertiesOnly()
	{
		var config = new MapperConfiguration(cfg => { });

		using var context = CreateContext();
		SeedData(context);

		// No type map registered - ProjectTo receives null typeMap
		// and should still attempt convention-based projection
		var act = () => context.Sources
			.ProjectTo<SimpleDestination>(config)
			.ToList();

		// Depending on implementation, this may throw or return convention-mapped results
		act.Should().NotThrow();
	}

	// --- MS-24516: nested child-collection / reference-navigation projection ---

	[Fact]
	public void ProjectTo_NestedChildCollection_ICollection_ProjectsElements()
	{
		var config = new MapperConfiguration(cfg => cfg.AddProfile(new NestedCollectionProjectProfile()));

		using var context = CreateContext();
		SeedParentWithChildren(context);

		var projected = context.ProjParents
			.ProjectTo<ProjParentDto>(config)
			.ToList();

		projected.Should().ContainSingle();
		var parent = projected[0];
		parent.Id.Should().Be(1);
		parent.Name.Should().Be("Sub-A");
		parent.Children.Should().HaveCount(2);
		parent.Children.Should().ContainSingle(c => c.Sku == "ENT" && c.Seats == 5);
		parent.Children.Should().ContainSingle(c => c.Sku == "DEV" && c.Seats == 3);
	}

	[Fact]
	public void ProjectTo_NestedChildCollection_List_ProjectsElements()
	{
		var config = new MapperConfiguration(cfg => cfg.AddProfile(new NestedCollectionProjectProfile()));

		using var context = CreateContext();
		SeedParentWithChildren(context);

		var projected = context.ProjParents
			.ProjectTo<ProjParentListDto>(config)
			.ToList();

		projected.Should().ContainSingle();
		projected[0].Children.Should().HaveCount(2);
		projected[0].Children.Select(c => c.Sku).Should().BeEquivalentTo(["ENT", "DEV"]);
	}

	[Fact]
	public void ProjectTo_NestedChildCollection_Array_ProjectsElements()
	{
		var config = new MapperConfiguration(cfg => cfg.AddProfile(new NestedCollectionProjectProfile()));

		using var context = CreateContext();
		SeedParentWithChildren(context);

		var projected = context.ProjParents
			.ProjectTo<ProjParentArrayDto>(config)
			.ToList();

		projected.Should().ContainSingle();
		projected[0].Children.Should().HaveCount(2);
		projected[0].Children.Select(c => c.Seats).Should().BeEquivalentTo([5, 3]);
	}

	[Fact]
	public void ProjectTo_NestedChildCollection_IEnumerable_ProjectsElements()
	{
		var config = new MapperConfiguration(cfg => cfg.AddProfile(new NestedCollectionProjectProfile()));

		using var context = CreateContext();
		SeedParentWithChildren(context);

		var projected = context.ProjParents
			.ProjectTo<ProjParentEnumerableDto>(config)
			.ToList();

		projected.Should().ContainSingle();
		projected[0].Children.Should().HaveCount(2);
	}

	[Fact]
	public void ProjectTo_NestedChildCollection_Empty_ProjectsEmptyCollection()
	{
		var config = new MapperConfiguration(cfg => cfg.AddProfile(new NestedCollectionProjectProfile()));

		using var context = CreateContext();
		context.ProjParents.Add(new ProjParentEntity { Id = 9, Name = "No-children" });
		context.SaveChanges();

		var projected = context.ProjParents
			.ProjectTo<ProjParentDto>(config)
			.ToList();

		projected.Should().ContainSingle();
		projected[0].Children.Should().BeEmpty();
	}

	[Fact]
	public void ProjectTo_NestedReferenceNavigation_ProjectsNestedObject()
	{
		var config = new MapperConfiguration(cfg => cfg.AddProfile(new NestedReferenceProjectProfile()));

		using var context = CreateContext();
		context.ProjOrders.Add(new ProjOrderEntity
		{
			Id = 1,
			Customer = "Acme",
			Address = new ProjAddressEntity { Id = 1, Street = "1 High St", City = "Springfield" }
		});
		context.SaveChanges();

		var projected = context.ProjOrders
			.ProjectTo<ProjOrderDto>(config)
			.ToList();

		projected.Should().ContainSingle();
		projected[0].Customer.Should().Be("Acme");
		projected[0].Address.Should().NotBeNull();
		projected[0].Address!.Street.Should().Be("1 High St");
		projected[0].Address!.City.Should().Be("Springfield");
	}

	[Fact]
	public void ProjectTo_NestedReferenceNavigation_Null_ProjectsNull()
	{
		var config = new MapperConfiguration(cfg => cfg.AddProfile(new NestedReferenceProjectProfile()));

		using var context = CreateContext();
		context.ProjOrders.Add(new ProjOrderEntity { Id = 2, Customer = "No-address", Address = null });
		context.SaveChanges();

		var projected = context.ProjOrders
			.ProjectTo<ProjOrderDto>(config)
			.ToList();

		projected.Should().ContainSingle();
		projected[0].Address.Should().BeNull();
	}

	[Fact]
	public void ProjectTo_NestedChildCollection_MatchesInMemoryMap()
	{
		var config = new MapperConfiguration(cfg => cfg.AddProfile(new NestedCollectionProjectProfile()));
		var mapper = config.CreateMapper();

		using var context = CreateContext();
		SeedParentWithChildren(context);

		var entity = context.ProjParents.Include(p => p.Children).Single();
		var mapped = mapper.Map<ProjParentDto>(entity);
		var projected = context.ProjParents.ProjectTo<ProjParentDto>(config).Single();

		projected.Children.Select(c => (c.Sku, c.Seats))
			.Should().BeEquivalentTo(mapped.Children.Select(c => (c.Sku, c.Seats)));
	}

	// --- MS-24516: ExplicitExpansion opt-out / membersToExpand opt-in ---

	[Fact]
	public void ProjectTo_ExplicitExpansion_ExcludesMemberByDefault()
	{
		var config = new MapperConfiguration(cfg => cfg.AddProfile(new ExplicitExpansionProfile()));

		using var context = CreateContext();
		SeedParentWithChildren(context);

		var projected = context.ProjParents
			.ProjectTo<ProjParentDto>(config)
			.ToList();

		projected.Should().ContainSingle();
		projected[0].Name.Should().Be("Sub-A");
		projected[0].Children.Should().BeEmpty("ExplicitExpansion excludes the member unless it is requested");
	}

	[Fact]
	public void ProjectTo_ExplicitExpansion_IncludesMemberWhenRequested()
	{
		var config = new MapperConfiguration(cfg => cfg.AddProfile(new ExplicitExpansionProfile()));

		using var context = CreateContext();
		SeedParentWithChildren(context);

		var projected = context.ProjParents
			.ProjectTo<ProjParentDto>(config, p => p.Children)
			.ToList();

		projected.Should().ContainSingle();
		projected[0].Children.Should().HaveCount(2);
		projected[0].Children.Should().ContainSingle(c => c.Sku == "ENT" && c.Seats == 5);
	}

	[Fact]
	public void Map_ExplicitExpansion_StillMapsMember()
	{
		// ExplicitExpansion only affects ProjectTo; the in-memory Map path must still populate it.
		var config = new MapperConfiguration(cfg => cfg.AddProfile(new ExplicitExpansionProfile()));
		var mapper = config.CreateMapper();

		var entity = new ProjParentEntity
		{
			Id = 1,
			Name = "Sub-A",
			Children = { new ProjChildEntity { Id = 1, Sku = "ENT", Seats = 5 } }
		};

		var dto = mapper.Map<ProjParentDto>(entity);

		dto.Children.Should().ContainSingle();
		dto.Children.First().Sku.Should().Be("ENT");
	}

	[Fact]
	public void ProjectTo_SelfReferential_NoMaxDepth_StopsAtCycle()
	{
		var config = new MapperConfiguration(cfg => cfg.AddProfile(new TreeProjectProfile()));

		using var context = CreateContext();
		SeedTree(context);

		var projected = context.ProjTrees
			.Where(t => t.Id == 1)
			.ProjectTo<ProjTreeDto>(config)
			.Single();

		projected.Name.Should().Be("Root");
		projected.Child.Should().NotBeNull();
		projected.Child!.Name.Should().Be("Child");
		projected.Child.Child.Should().BeNull("recursion stops at the first cycle when no MaxDepth is configured");
	}

	[Fact]
	public void ProjectTo_SelfReferential_WithMaxDepth_ExpandsToConfiguredDepth()
	{
		var config = new MapperConfiguration(cfg => cfg.AddProfile(new TreeMaxDepthProjectProfile()));

		using var context = CreateContext();
		SeedTree(context);

		var projected = context.ProjTrees
			.Where(t => t.Id == 1)
			.ProjectTo<ProjTreeDto>(config)
			.Single();

		projected.Name.Should().Be("Root");
		projected.Child!.Name.Should().Be("Child");
		projected.Child.Child!.Name.Should().Be("Grandchild");
		projected.Child.Child.Child.Should().BeNull("MaxDepth(2) expands two levels of children");
	}

	// --- Backward-compatibility guards: members that already worked must be unchanged ---

	[Fact]
	public void ProjectTo_SameTypeComplexMembers_NoElementMap_PassThroughUnchanged()
	{
		// Marker/Tags reuse the SAME complex type on source and destination with no element map, and
		// Notes is a collection of primitives. None of these need element-wise mapping; ProjectTo must
		// copy them straight through, exactly as it did before nested projection was added.
		var config = new MapperConfiguration(cfg => cfg.AddProfile(new PassThroughProfile()));

		var source = new List<PassThroughSource>
		{
			new()
			{
				Id = 1,
				Marker = new PassThroughTag { Label = "m" },
				Tags = { new PassThroughTag { Label = "vip" } },
				Notes = { "a", "b" }
			}
		}.AsQueryable();

		var projected = source.ProjectTo<PassThroughDest>(config).ToList();

		projected.Should().ContainSingle();
		projected[0].Id.Should().Be(1);
		projected[0].Marker.Should().NotBeNull();
		projected[0].Marker!.Label.Should().Be("m");
		projected[0].Tags.Should().ContainSingle();
		projected[0].Tags[0].Label.Should().Be("vip");
		projected[0].Notes.Should().BeEquivalentTo(["a", "b"]);
	}

	[Fact]
	public void AssertConfigurationIsValid_WithExplicitExpansion_DoesNotThrow()
	{
		var config = new MapperConfiguration(cfg => cfg.AddProfile(new ExplicitExpansionProfile()));

		var act = config.AssertConfigurationIsValid;

		act.Should().NotThrow();
	}

	private static void SeedTree(TestDbContext context)
	{
		var grandchild = new ProjTreeEntity { Id = 3, Name = "Grandchild" };
		var child = new ProjTreeEntity { Id = 2, Name = "Child", Child = grandchild };
		var root = new ProjTreeEntity { Id = 1, Name = "Root", Child = child };
		context.ProjTrees.AddRange(grandchild, child, root);
		context.SaveChanges();
	}

	private static void SeedParentWithChildren(TestDbContext context)
	{
		context.ProjParents.Add(new ProjParentEntity
		{
			Id = 1,
			Name = "Sub-A",
			Children =
			{
				new ProjChildEntity { Id = 1, Sku = "ENT", Seats = 5 },
				new ProjChildEntity { Id = 2, Sku = "DEV", Seats = 3 }
			}
		});
		context.SaveChanges();
	}

	private class NestedCollectionProjectProfile : Profile
	{
		public NestedCollectionProjectProfile()
		{
			CreateMap<ProjChildEntity, ProjChildDto>();
			CreateMap<ProjParentEntity, ProjParentDto>();
			CreateMap<ProjParentEntity, ProjParentListDto>();
			CreateMap<ProjParentEntity, ProjParentArrayDto>();
			CreateMap<ProjParentEntity, ProjParentEnumerableDto>();
		}
	}

	private class NestedReferenceProjectProfile : Profile
	{
		public NestedReferenceProjectProfile()
		{
			CreateMap<ProjAddressEntity, ProjAddressDto>();
			CreateMap<ProjOrderEntity, ProjOrderDto>();
		}
	}

	private class ExplicitExpansionProfile : Profile
	{
		public ExplicitExpansionProfile()
		{
			CreateMap<ProjChildEntity, ProjChildDto>();
			CreateMap<ProjParentEntity, ProjParentDto>()
				.ForMember(d => d.Children, opt => opt.ExplicitExpansion());
		}
	}

	private class TreeProjectProfile : Profile
	{
		public TreeProjectProfile() => CreateMap<ProjTreeEntity, ProjTreeDto>();
	}

	private class TreeMaxDepthProjectProfile : Profile
	{
		public TreeMaxDepthProjectProfile() => CreateMap<ProjTreeEntity, ProjTreeDto>().MaxDepth(2);
	}

	private class PassThroughProfile : Profile
	{
		public PassThroughProfile() => CreateMap<PassThroughSource, PassThroughDest>();
	}
}

public class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
{
	public DbSet<SimpleSource> Sources { get; set; } = null!;
	public DbSet<PersonSource> Persons { get; set; } = null!;
	public DbSet<NullableDoubleEntity> NullableDoubles { get; set; } = null!;
	public DbSet<NullablePortEntity> NullablePorts { get; set; } = null!;
	public DbSet<ProjParentEntity> ProjParents { get; set; } = null!;
	public DbSet<ProjChildEntity> ProjChildren { get; set; } = null!;
	public DbSet<ProjOrderEntity> ProjOrders { get; set; } = null!;
	public DbSet<ProjAddressEntity> ProjAddresses { get; set; } = null!;
	public DbSet<ProjTreeEntity> ProjTrees { get; set; } = null!;

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.Entity<SimpleSource>().HasKey(e => e.Id);
		modelBuilder.Entity<PersonSource>().HasKey(e => e.FirstName);
		modelBuilder.Entity<NullableDoubleEntity>().HasKey(e => e.Id);
		modelBuilder.Entity<NullablePortEntity>().HasKey(e => e.Id);
		modelBuilder.Entity<ProjParentEntity>().HasKey(e => e.Id);
		modelBuilder.Entity<ProjChildEntity>().HasKey(e => e.Id);
		modelBuilder.Entity<ProjParentEntity>()
			.HasMany(e => e.Children)
			.WithOne()
			.HasForeignKey(e => e.ProjParentEntityId);
		modelBuilder.Entity<ProjAddressEntity>().HasKey(e => e.Id);
		modelBuilder.Entity<ProjOrderEntity>().HasKey(e => e.Id);
		modelBuilder.Entity<ProjOrderEntity>()
			.HasOne(e => e.Address)
			.WithMany()
			.HasForeignKey(e => e.AddressId);
		modelBuilder.Entity<ProjTreeEntity>().HasKey(e => e.Id);
		modelBuilder.Entity<ProjTreeEntity>()
			.HasOne(e => e.Child)
			.WithMany()
			.HasForeignKey(e => e.ChildId);
	}
}