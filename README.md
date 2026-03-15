# PanoramicData.Mapper

A minimal, MIT-licensed, API-compatible replacement for AutoMapper.

[![Nuget](https://img.shields.io/nuget/v/PanoramicData.Mapper)](https://www.nuget.org/packages/PanoramicData.Mapper/)
[![Nuget](https://img.shields.io/nuget/dt/PanoramicData.Mapper)](https://www.nuget.org/packages/PanoramicData.Mapper/)
![License](https://img.shields.io/github/license/panoramicdata/PanoramicData.Mapper)
[![Codacy Badge](https://app.codacy.com/project/badge/Grade/95dd8997738d4ffb8daf60c1b85a605d)](https://app.codacy.com/gh/panoramicdata/PanoramicData.Mapper/dashboard?utm_source=gh&utm_medium=referral&utm_content=&utm_campaign=Badge_grade)

## Overview

PanoramicData.Mapper provides a subset of AutoMapper's API surface sufficient for convention-based object mapping with explicit member configuration.
It is a clean-room, black-box reimplementation — no AutoMapper source code was referenced.

## Supported Features

- **Convention-based mapping** — properties with matching names and compatible types map automatically
- **Profile-based configuration** — derive from `Profile` and call `CreateMap<TSource, TDestination>()`
- **ForMember / Ignore** — skip specific destination properties
- **ForMember / MapFrom** — custom source expressions including nested property access
- **AfterMap** — inline lambda and generic `IMappingAction<TSrc, TDest>` post-mapping callbacks
- **ForAllMembers** — apply configuration to all destination members
- **Map to new** — `mapper.Map<TDest>(source)` creates a new destination
- **Map to existing** — `mapper.Map(source, destination)` updates an existing object
- **ProjectTo** — `IQueryable<T>.ProjectTo<TDest>(configurationProvider)` for EF Core SQL projection
- **[Ignore] attribute** — `PanoramicData.Mapper.Configuration.Annotations.IgnoreAttribute`
- **Nested mappings** — recursive mapping of complex child types and collection properties when a CreateMap exists for the child types
- **Collection/List/Array mapping** — `mapper.Map<List<Dest>>(sourceList)` maps collections automatically when an element-type map is registered
- **Flattening** — PascalCase destination property names are split and traversed on the source graph (e.g. `CustomerName` → `Customer.Name`); also matches `GetX()` methods
- **AssertConfigurationIsValid** — detects unmapped destination properties at startup
- **DI integration** — `AddAutoMapper()` extension methods for `IServiceCollection`

## Installation

```bash
dotnet add package PanoramicData.Mapper
```

## Quick Start

```csharp
using PanoramicData.Mapper;

// Define a profile
public class MyProfile : Profile
{
    public MyProfile()
    {
        CreateMap<Source, Destination>()
            .ForMember(d => d.Secret, opt => opt.Ignore())
            .ForMember(d => d.FullName, opt => opt.MapFrom(s => s.FirstName + " " + s.LastName));
    }
}

// Configure and use
var config = new MapperConfiguration(cfg => cfg.AddProfile<MyProfile>());
config.AssertConfigurationIsValid();

IMapper mapper = config.CreateMapper();
var dest = mapper.Map<Destination>(source);
```

## DI Registration

```csharp
// In Program.cs / Startup.cs
services.AddAutoMapper(typeof(MyProfile).Assembly);

// Or with explicit configuration
services.AddAutoMapper(cfg => cfg.AddProfile<MyProfile>());
```

## Migration from AutoMapper

1. Replace the `AutoMapper` NuGet package with `PanoramicData.Mapper`
2. Update `using` directives:
   - `using AutoMapper;` → `using PanoramicData.Mapper;`
   - `using AutoMapper.QueryableExtensions;` → `using PanoramicData.Mapper.QueryableExtensions;`
   - `using AutoMapper.Configuration.Annotations;` → `using PanoramicData.Mapper.Configuration.Annotations;`
3. The `AddAutoMapper()` DI extension methods and all type names (`Profile`, `IMapper`, `MapperConfiguration`, etc.) remain the same

## Out of Scope

Features not currently implemented:
- `IValueResolver`, `ITypeConverter`, `ConvertUsing`, `ConstructUsing`
- `ReverseMap`, `ForCtorParam`, `PreCondition`, `Condition`
- `NullSubstitute`, `UseDestinationValue`, `ForPath`, `MaxDepth`
- `IncludeBase`, `IncludeAllDerived`
- Open generics
- Async mapping
- BeforeMap (AfterMap is supported)

## License

MIT — see [LICENSE](LICENSE) for details.
