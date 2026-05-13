# HomeManager API — Esqueleto del módulo Inventario

**Fecha:** 2026-05-12
**Alcance:** Solo estructura de proyectos, archivos y configuración inicial. Sin lógica de negocio, sin propiedades en entidades, sin implementaciones en métodos.

## Objetivo

Generar el esqueleto base de una API REST en .NET 10 para gestión doméstica, comenzando por el módulo de Inventario. La estructura debe seguir principios SOLID y Clean Architecture en 4 capas, usar SQL Server con Entity Framework Core, y exponer Swagger para documentación y pruebas.

## Stack técnico

- **.NET 10** (SDK 10.0.103 disponible localmente)
- **Entity Framework Core 10** + **SQL Server**
- **AutoMapper** (mapeo DTO ↔ entidad)
- **FluentValidation** (validación de DTOs)
- **Swashbuckle.AspNetCore** (Swagger / OpenAPI)
- **xUnit + Moq + FluentAssertions** (testing)

## Arquitectura

Clean Architecture en 4 capas, con regla de dependencias hacia el centro:

```
API ─────────► Application ─────────► Domain
 │                                       ▲
 └──► Infrastructure ────────────────────┘
```

- **Domain** no referencia ningún otro proyecto.
- **Application** referencia solo a Domain.
- **Infrastructure** referencia a Application y a Domain (implementa interfaces).
- **API** referencia a Application e Infrastructure (esta última solo para registrar DI).

## Estructura de la solución

```
HomeManager-API/
├── HomeManager.sln
├── src/
│   ├── HomeManager.Domain/
│   ├── HomeManager.Application/
│   ├── HomeManager.Infrastructure/
│   └── HomeManager.API/
└── tests/
    └── HomeManager.Tests/
```

## Capa: Domain (`src/HomeManager.Domain/`)

```
Domain/
├── Common/
│   └── BaseEntity.cs
├── Entities/
│   ├── Category.cs
│   ├── Product.cs
│   ├── Storage.cs
│   └── Inventory.cs
└── Interfaces/
    ├── IRepository.cs
    └── IUnitOfWork.cs
```

**Reglas:**
- Todos los archivos contienen únicamente la declaración de clase/interfaz vacía (sin propiedades, sin métodos).
- `BaseEntity` es `abstract`.
- `IRepository<T>` es genérica con restricción `where T : BaseEntity`.
- Sin paquetes NuGet.

## Capa: Application (`src/HomeManager.Application/`)

```
Application/
├── DTOs/
│   ├── Category/
│   │   ├── CategoryDto.cs
│   │   ├── CreateCategoryDto.cs
│   │   └── UpdateCategoryDto.cs
│   ├── Product/
│   │   ├── ProductDto.cs
│   │   ├── CreateProductDto.cs
│   │   └── UpdateProductDto.cs
│   ├── Storage/
│   │   ├── StorageDto.cs
│   │   ├── CreateStorageDto.cs
│   │   └── UpdateStorageDto.cs
│   └── Inventory/
│       ├── InventoryDto.cs
│       ├── CreateInventoryDto.cs
│       └── UpdateInventoryDto.cs
├── Interfaces/
│   ├── ICategoryService.cs
│   ├── IProductService.cs
│   ├── IStorageService.cs
│   └── IInventoryService.cs
├── Services/
│   ├── CategoryService.cs
│   ├── ProductService.cs
│   ├── StorageService.cs
│   └── InventoryService.cs
├── Mappings/
│   └── MappingProfile.cs
├── Validators/
│   ├── Category/
│   │   ├── CreateCategoryValidator.cs
│   │   └── UpdateCategoryValidator.cs
│   ├── Product/
│   │   ├── CreateProductValidator.cs
│   │   └── UpdateProductValidator.cs
│   ├── Storage/
│   │   ├── CreateStorageValidator.cs
│   │   └── UpdateStorageValidator.cs
│   └── Inventory/
│       ├── CreateInventoryValidator.cs
│       └── UpdateInventoryValidator.cs
└── DependencyInjection.cs
```

**Reglas:**
- DTOs son `record` o `class` vacíos.
- Interfaces de servicios declaradas sin miembros.
- Clases `*Service` implementan su interfaz sin contenido en métodos (interfaces vacías, así que no hay nada que implementar).
- `MappingProfile` hereda de `AutoMapper.Profile` con constructor vacío.
- `*Validator` heredan de `AbstractValidator<T>` con constructor vacío.
- `DependencyInjection.cs` expone `public static IServiceCollection AddApplication(this IServiceCollection services)` que retorna `services` (sin registros aún).

**Paquetes NuGet:**
- `AutoMapper`
- `AutoMapper.Extensions.Microsoft.DependencyInjection`
- `FluentValidation`
- `FluentValidation.DependencyInjectionExtensions`

## Capa: Infrastructure (`src/HomeManager.Infrastructure/`)

```
Infrastructure/
├── Persistence/
│   ├── HomeManagerDbContext.cs
│   └── Configurations/
│       ├── CategoryConfiguration.cs
│       ├── ProductConfiguration.cs
│       ├── StorageConfiguration.cs
│       └── InventoryConfiguration.cs
├── Repositories/
│   ├── Repository.cs
│   └── UnitOfWork.cs
└── DependencyInjection.cs
```

**Reglas:**
- `HomeManagerDbContext` hereda de `DbContext`, con constructor que recibe `DbContextOptions<HomeManagerDbContext>`. Sin `DbSet` declarados todavía.
- Cada `*Configuration` implementa `IEntityTypeConfiguration<T>` con método `Configure` vacío.
- `Repository<T>` implementa `IRepository<T>` con constructor que recibe `HomeManagerDbContext` y sin métodos (la interfaz está vacía).
- `UnitOfWork` implementa `IUnitOfWork` con constructor que recibe `HomeManagerDbContext`.
- `DependencyInjection.cs` expone `public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)` que retorna `services` (sin registros aún).
- La carpeta `Migrations/` no se crea manualmente; EF la generará en el primer `dotnet ef migrations add`.

**Paquetes NuGet:**
- `Microsoft.EntityFrameworkCore`
- `Microsoft.EntityFrameworkCore.SqlServer`
- `Microsoft.EntityFrameworkCore.Design`
- `Microsoft.EntityFrameworkCore.Tools`

## Capa: API (`src/HomeManager.API/`)

```
API/
├── Controllers/
│   ├── CategoriesController.cs
│   ├── ProductsController.cs
│   ├── StoragesController.cs
│   └── InventoriesController.cs
├── Properties/
│   └── launchSettings.json
├── appsettings.json
├── appsettings.Development.json
├── Program.cs
└── HomeManager.API.csproj
```

**Reglas:**
- Cada controller hereda de `ControllerBase`, decorado con `[ApiController]` y `[Route("api/[controller]")]`, sin endpoints.
- `Program.cs`:
  - Registra `AddControllers()`, `AddEndpointsApiExplorer()`, `AddSwaggerGen()`.
  - Llama a `builder.Services.AddApplication()` y `builder.Services.AddInfrastructure(builder.Configuration)`.
  - Habilita Swagger UI en entorno de desarrollo.
  - Mapea controllers.
- `appsettings.json` incluye sección `ConnectionStrings.DefaultConnection` con placeholder apuntando a `Server=localhost;Database=HomeManager;Trusted_Connection=True;TrustServerCertificate=True;`.

**Paquetes NuGet:**
- `Swashbuckle.AspNetCore`
- `Microsoft.EntityFrameworkCore.Design` (para que `dotnet ef` resuelva el design-time DbContext desde este proyecto)

## Proyecto de tests (`tests/HomeManager.Tests/`)

```
Tests/
├── Domain/
├── Application/
│   └── Services/
├── Infrastructure/
│   └── Repositories/
└── HomeManager.Tests.csproj
```

**Reglas:**
- Sin archivos de test. Solo las carpetas espejo y el csproj con los paquetes.
- Referencia los 3 proyectos de `src/` para tener acceso a todas las capas.

**Paquetes NuGet:**
- `xunit`
- `xunit.runner.visualstudio`
- `Microsoft.NET.Test.Sdk`
- `Moq`
- `FluentAssertions`

## Criterios de aceptación

1. `dotnet build` desde la raíz compila la solución completa sin errores ni warnings.
2. `dotnet run --project src/HomeManager.API` levanta la API y `https://localhost:<puerto>/swagger` muestra la UI de Swagger (sin endpoints listados, lo cual es esperado).
3. `dotnet test` ejecuta el runner de tests sin tests ni fallos.
4. Las dependencias entre proyectos respetan la regla de Clean Architecture (validable con `dotnet list reference`).
5. Ningún archivo contiene lógica de negocio: las clases están vacías o solo con constructores que asignan dependencias inyectadas.

## Fuera de alcance

- Autenticación / autorización.
- Logging configurado (más allá del default).
- CORS configurado.
- Manejo global de errores / middlewares custom.
- Health checks.
- Docker / docker-compose.
- CI/CD.
- Otros módulos del HomeManager (finanzas, tareas, etc.). Solo Inventario.
