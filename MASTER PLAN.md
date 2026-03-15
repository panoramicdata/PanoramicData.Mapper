# PanoramicData.Mapper - MASTER PLAN

## What This Is
An almost-drop-in replacement for AutoMapper. Consuming projects need to:
1. Swap the NuGet reference from `AutoMapper` / `AutoMapper.Extensions.Microsoft.DependencyInjection` to `PanoramicData.Mapper`
2. Change `using AutoMapper;` to `using PanoramicData.Mapper;` Hint: use a `GlobalUsings.cs` to permit testing the swap-out in one place.

All types live in **PanoramicData.Mapper namespaces**:
- `PanoramicData.Mapper` - IMapper, Mapper, MapperConfiguration, Profile, IMappingExpression, etc.
- `PanoramicData.Mapper.Configuration.Annotations` - `[Ignore]` attribute
- `PanoramicData.Mapper.QueryableExtensions` - `ProjectTo<T>()` extension
- `PanoramicData.Mapper.Internal` - TypeMap, PropertyMapping (internal engine)
- `Microsoft.Extensions.DependencyInjection` - `AddAutoMapper()` extensions (unchanged, standard DI namespace)

## Current State

### Library builds clean (TreatWarningsAsErrors enabled)
### All tests pass
### Namespaces set to PanoramicData.Mapper
### Publish.ps1 created (NuGet API key file is gitignored)

---

## Remaining Work - Priority Order

### P0: Pre-commit - COMPLETE

1. Fix failing test - DONE
2. Create missing test files (ProfileRegistration, AssertConfigurationIsValid, DependencyInjection) - DONE
3. Change namespaces to PanoramicData.Mapper - DONE
4. Create Publish.ps1 - DONE
5. Add TreatWarningsAsErrors - DONE
6. Add performance tests - DONE
7. Git commit and push

### P1: Enable NuGet publishing

1. Set `GeneratePackageOnBuild` to `true` in csproj
2. Verify `dotnet pack` produces a valid .nupkg + .snupkg
3. Publish to NuGet.org using `.\Publish.ps1`

### P2: Feature gaps (from AutoMapper documentation review) - COMPLETE

Reviewed against https://docs.automapper.org/en/stable/ and implemented:

- **Nested Mappings** - When a destination property is a complex type with its own CreateMap, the mapper recursively maps. TypeMap detects mapped child types via TypeMapResolver and invokes their mapping. Also handles collection-typed properties (e.g., `List<ChildSource>` ? `List<ChildDest>`). - DONE
- **List/Array mapping** - `mapper.Map<List<Dest>>(sourceList)` automatically maps collections if the element type has a map. Supports `List<T>`, `T[]`, and interface types. - DONE
- **Flattening** - Splits PascalCase destination property names and traverses source graph (e.g., `CustomerName` matches `Customer.Name`). Also supports `GetX()` method matching and deep nesting (e.g., `OrderItemName` ? `Order.Item.Name`). - DONE

### P3: Quality and CI

1. Codacy integration - add badge to README - DONE
2. Code coverage - `coverlet.collector` already referenced in test project; enable reporting in CI
3. GitHub Actions CI - build + test + coverage upload on PR/push

### P4: Remaining AutoMapper features

All features previously marked "out of scope" are now planned:

1. **ReverseMap** - `.ReverseMap()` creates the inverse mapping automatically
2. **BeforeMap** - `.BeforeMap((src, dst) => ...)` pre-mapping callback (AfterMap already implemented)
3. **Conditional Mapping** - `.Condition()` and `.PreCondition()` on ForMember
4. **Null Substitution** - `.NullSubstitute(value)` on ForMember
5. **Value Resolvers** - `IValueResolver<TSrc, TDst, TMember>` for custom resolution logic
6. **Type Converters** - `ITypeConverter<TSrc, TDst>` and `ConvertUsing()` for full-type conversion
7. **Construction** - `ConstructUsing()` for custom destination construction
8. **ForPath** - `.ForPath(d => d.Inner.Prop, opt => ...)` for deep member configuration
9. **ForCtorParam** - `.ForCtorParam("paramName", opt => ...)` for constructor parameter mapping
10. **Mapping Inheritance** - `Include`, `IncludeBase`, `IncludeAllDerived` for polymorphic hierarchies
11. **Value Transformers** - Global/profile-level value transforms
12. **Open Generics** - `CreateMap(typeof(Source<>), typeof(Dest<>))`
13. **UseDestinationValue** - `.UseDestinationValue()` to preserve existing destination collection instances
14. **MaxDepth** - `.MaxDepth(n)` to limit recursive mapping depth
15. **Async mapping** - async-aware mapping pipeline

---

## API Surface Coverage

| AutoMapper Feature | Implemented | Tested |
|---|---|---|
| `CreateMap<S,D>()` | Yes | Yes |
| `.ForMember(expr, opt => opt.Ignore())` | Yes | Yes |
| `.ForMember(expr, opt => opt.MapFrom(...))` | Yes | Yes |
| `.ForMember(string, opt => ...)` | Yes | Yes |
| `.AfterMap((src, dst) => ...)` | Yes | Yes |
| `.AfterMap<TMappingAction>()` | Yes | Yes |
| `.ForAllMembers(opt => opt.Ignore())` | Yes | Yes |
| `IMapper.Map<T>(object)` | Yes | Yes |
| `IMapper.Map<S,D>(src)` | Yes | Yes |
| `IMapper.Map<S,D>(src, dest)` | Yes | Yes |
| `IMapper.Map(obj, Type, Type)` | Yes | Yes |
| `IMapper.Map(obj, obj, Type, Type)` | Yes | Yes |
| `ProjectTo<T>(configProvider)` | Yes | Yes |
| `[Ignore]` attribute | Yes | Yes |
| `AddAutoMapper(typeof(...))` | Yes | Yes |
| `AssertConfigurationIsValid()` | Yes | Yes |
| `IConfigurationProvider` (for ProjectTo) | Yes | Yes |
| Nested mappings (complex child types) | Yes | Yes |
| Collection/List/Array mapping | Yes | Yes |
| Flattening (PascalCase split + GetX()) | Yes | Yes |

---

## Key Design Decisions

1. **All types in `PanoramicData.Mapper` namespace** - consuming projects swap the NuGet reference and update `using` directives
2. **net10.0 only** - no netstandard2.0 (simplified for modern consumers)
3. **Reflection-based engine** (not source generators) - simpler, matches AutoMapper runtime behavior
4. **Compiled mapper caching** - TypeMap compiles property assignments once, reuses for subsequent maps
5. **Expression tree projection** - ProjectTo builds real Expression trees that EF Core can translate to SQL
6. **nbgv versioning** - consistent with all PanoramicData repos
7. **xunit.v3 + AwesomeAssertions** - consistent with PanoramicData test conventions