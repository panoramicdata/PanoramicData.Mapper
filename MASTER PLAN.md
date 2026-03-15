# PanoramicData.Mapper - MASTER PLAN

## What This Is
A comprehensive, API-compatible replacement for AutoMapper. Consuming projects need to:
1. Swap the NuGet reference from `AutoMapper` / `AutoMapper.Extensions.Microsoft.DependencyInjection` to `PanoramicData.Mapper`
2. Change `using AutoMapper;` to `using PanoramicData.Mapper;` Hint: use a `GlobalUsings.cs` to permit testing the swap-out in one place.

All types live in **PanoramicData.Mapper namespaces**:
- `PanoramicData.Mapper` — IMapper, Mapper, MapperConfiguration, Profile, IMappingExpression, IValueResolver, ITypeConverter, IMappingAction, ResolutionContext, etc.
- `PanoramicData.Mapper.Configuration.Annotations` — `[Ignore]` attribute
- `PanoramicData.Mapper.QueryableExtensions` — `ProjectTo<T>()` extension
- `PanoramicData.Mapper.Internal` — TypeMap, PropertyMapping (internal engine)
- `Microsoft.Extensions.DependencyInjection` — `AddAutoMapper()` extensions (unchanged, standard DI namespace)

## Current State (v10.0.7)

- Library builds clean (`TreatWarningsAsErrors` enabled)
- **90 tests** — all passing
- Published to [NuGet.org](https://www.nuget.org/packages/PanoramicData.Mapper/) as v10.0.7
- Codacy grade: **A** — all complexity and best-practice issues resolved
- GitHub Actions CI: build + test + coverage on push/PR; CodeQL weekly + on push/PR
- GitHub issue #1 (IgnoreAllPropertiesWithAnInaccessibleSetter) — **closed**
- No open issues

---

## Completed Work

### P0: Pre-commit ?

1. Fix failing test
2. Create missing test files (ProfileRegistration, AssertConfigurationIsValid, DependencyInjection)
3. Change namespaces to PanoramicData.Mapper
4. Create Publish.ps1
5. Add TreatWarningsAsErrors
6. Add performance tests
7. Git commit and push

### P1: NuGet publishing ?

1. Set `GeneratePackageOnBuild` to `true` in csproj
2. Verified `dotnet pack` produces a valid .nupkg + .snupkg
3. Published to NuGet.org via `.\Publish.ps1`

### P2: Feature gaps ?

Reviewed against https://docs.automapper.org/en/stable/ and implemented:

- **Nested Mappings** — recursive mapping of complex child types; collection-typed properties (e.g., `List<ChildSource>` ? `List<ChildDest>`)
- **List/Array mapping** — `mapper.Map<List<Dest>>(sourceList)` for `List<T>`, `T[]`, and interface types
- **Flattening** — PascalCase splitting + `GetX()` method matching + deep nesting (e.g., `OrderItemName` ? `Order.Item.Name`)

### P3: Quality and CI ?

1. Codacy integration — badge in README, grade A
2. Code coverage — coverlet collects Cobertura XML; Codacy coverage reporter uploads on main push
3. GitHub Actions CI — build + test + coverage on push/PR; CodeQL weekly + on push/PR

### P4: Remaining AutoMapper features ?

All features previously marked "out of scope" are now implemented and tested:

1. **ReverseMap** — `.ReverseMap()` creates the inverse mapping automatically
2. **BeforeMap** — `.BeforeMap((src, dst) => ...)` pre-mapping callback (AfterMap already implemented)
3. **Conditional Mapping** — `.Condition()` and `.PreCondition()` on ForMember
4. **Null Substitution** — `.NullSubstitute(value)` on ForMember
5. **Value Resolvers** — `IValueResolver<TSrc, TDst, TMember>` for custom resolution logic
6. **Type Converters** — `ITypeConverter<TSrc, TDst>` and `ConvertUsing()` for full-type conversion
7. **Construction** — `ConstructUsing()` for custom destination construction
8. **ForPath** — `.ForPath(d => d.Inner.Prop, opt => ...)` for deep member configuration
9. **ForCtorParam** — `.ForCtorParam("paramName", opt => ...)` for constructor parameter mapping
10. **Mapping Inheritance** — `Include`, `IncludeBase`, `IncludeAllDerived` for polymorphic hierarchies
11. **Value Transformers** — Global/profile-level value transforms
12. **Open Generics** — `CreateMap(typeof(Source<>), typeof(Dest<>))`
13. **UseDestinationValue** — `.UseDestinationValue()` to preserve existing destination collection instances
14. **MaxDepth** — `.MaxDepth(n)` to limit recursive mapping depth
15. **Async mapping** — deferred (no async I/O in mapping pipeline; all mapping is synchronous by design)

### P5: Code quality fixes ?

- Refactored TypeMap.cs: extracted 10+ helper methods to reduce cyclomatic complexity (CC ? 8 per method)
- Refactored MapperConfiguration.cs: extracted `TryResolveInheritedMap`, `CreateAndCacheDerivedMap`, `TryResolveOpenGenericMap`
- Fixed 3 unused parameter warnings in test interface implementations

### P6: Extension methods ?

- **IgnoreAllPropertiesWithAnInaccessibleSetter** — extension method on `IMappingExpression<TSource, TDestination>` to ignore all destination properties with non-public or absent setters (GitHub issue #1)

---

## API Surface Coverage

| AutoMapper Feature | Implemented | Tested |
|---|---|---|
| `CreateMap<S,D>()` | ? | ? |
| `.ForMember(expr, opt => opt.Ignore())` | ? | ? |
| `.ForMember(expr, opt => opt.MapFrom(...))` | ? | ? |
| `.ForMember(string, opt => ...)` | ? | ? |
| `.AfterMap((src, dst) => ...)` | ? | ? |
| `.AfterMap<TMappingAction>()` | ? | ? |
| `.BeforeMap((src, dst) => ...)` | ? | ? |
| `.BeforeMap<TMappingAction>()` | ? | ? |
| `.ForAllMembers(opt => opt.Ignore())` | ? | ? |
| `.ReverseMap()` | ? | ? |
| `.ConvertUsing(lambda / ITypeConverter)` | ? | ? |
| `.ConstructUsing(lambda)` | ? | ? |
| `.ForPath(d => d.Inner.Prop, opt => ...)` | ? | ? |
| `.ForCtorParam("name", opt => ...)` | ? | ? |
| `.Condition() / .PreCondition()` | ? | ? |
| `.NullSubstitute(value)` | ? | ? |
| `.MapFrom<IValueResolver>()` | ? | ? |
| `.Include<TDerived>()` | ? | ? |
| `.IncludeBase<TBase>()` | ? | ? |
| `.IncludeAllDerived()` | ? | ? |
| `.MaxDepth(n)` | ? | ? |
| `.AddTransform<T>(expr)` | ? | ? |
| `.UseDestinationValue()` | ? | ? |
| `CreateMap(typeof(S<>), typeof(D<>))` | ? | ? |
| `IMapper.Map<T>(object)` | ? | ? |
| `IMapper.Map<S,D>(src)` | ? | ? |
| `IMapper.Map<S,D>(src, dest)` | ? | ? |
| `IMapper.Map(obj, Type, Type)` | ? | ? |
| `IMapper.Map(obj, obj, Type, Type)` | ? | ? |
| `ProjectTo<T>(configProvider)` | ? | ? |
| `[Ignore]` attribute | ? | ? |
| `AddAutoMapper(typeof(...))` | ? | ? |
| `AddAutoMapper(Assembly[])` | ? | ? |
| `AddAutoMapper(Action<IMapperConfigurationExpression>)` | ? | ? |
| `AssertConfigurationIsValid()` | ? | ? |
| `IConfigurationProvider` (for ProjectTo) | ? | ? |
| Nested mappings (complex child types) | ? | ? |
| Collection/List/Array mapping | ? | ? |
| Flattening (PascalCase split + GetX()) | ? | ? |
| `IgnoreAllPropertiesWithAnInaccessibleSetter()` | ? | ? |

---

## Key Design Decisions

1. **All types in `PanoramicData.Mapper` namespace** — consuming projects swap the NuGet reference and update `using` directives
2. **net10.0 only** — no netstandard2.0 (simplified for modern consumers)
3. **Reflection-based engine** (not source generators) — simpler, matches AutoMapper runtime behavior
4. **Compiled mapper caching** — TypeMap compiles property assignments once, reuses for subsequent maps
5. **Expression tree projection** — ProjectTo builds real Expression trees that EF Core can translate to SQL
6. **nbgv versioning** — consistent with all PanoramicData repos
7. **xunit.v3 + AwesomeAssertions** — consistent with PanoramicData test conventions
8. **Codacy-compliant complexity** — all methods ? CC 8; helpers extracted for readability