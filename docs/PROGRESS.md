# HomeManager-API — Estado de avance

> Documento de continuidad para retomar el trabajo desde cualquier equipo.
> Última actualización: 2026-06-06

---

## TL;DR — dónde estamos

- **Arquitectura:** Clean Architecture, 4 capas, .NET 10, EF Core 10 + SQL Server, AutoMapper, FluentValidation, Swagger.
- **Capa Domain:** ✅ completa (entidades con propiedades + interfaces de contrato).
- **Capa Application:** 🚧 en progreso — DTOs ✅, interfaces de servicio ✅, **implementación de servicios pendiente**.
- **Capas Infrastructure y API:** scaffolding vacío (clases creadas sin lógica).
- ⚠️ **El build está ROTO a propósito.** Los `*Service` declaran que implementan sus interfaces pero todavía no tienen los métodos. Es el siguiente paso planificado, no un bug.

---

## Cómo retomar la sesión de tutoría

Este proyecto se está construyendo en modo **tutor socrático** (una pregunta a la vez, el alumno escribe el código, el tutor no lo escribe por él). Para continuar, abrir el proyecto y escribir:

> **"Where did we leave off?"**

El tutor pedirá lo último que tuvo sentido y lo último que no, y retomará desde ahí.

---

## Estado por capa

### 1. Domain — ✅ COMPLETA

`src/HomeManager.Domain/`

| Archivo | Estado | Notas |
|---|---|---|
| `Common/BaseEntity.cs` | ✅ | `abstract`. `Id` (Guid), `CreatedAt`, `LastUpdatedAt`, `IsDeleted`. |
| `Entities/Category.cs` | ✅ | Solo `Name`. |
| `Entities/Product.cs` | ✅ | `Name`, `DaysFresh`, `Brand`, `Price` (decimal?), `Description`, `ImageUrl`, `IsActive`, `CategoryId` + navegación `Category?`. |
| `Entities/Storage.cs` | ✅ | Solo `Name`. |
| `Entities/Inventory.cs` | ✅ | Junction entre Product y Storage + ciclo de vida (`IsOpened`/`OpenedAt`, `IsConsumed`/`ConsumedAt`, `ExpirationDate`, `BestBeforeDate`). |
| `Interfaces/IRepository.cs` | ✅ | Genérico `where T : BaseEntity`. `CreateAsync`, `GetByIdAsync`, `GetAllAsync` (Task) + `Remove`, `EditById` (void). |
| `Interfaces/IUnitOfWork.cs` | ✅ | `SaveChangesAsync(CancellationToken)`. |

### 2. Application — 🚧 EN PROGRESO

`src/HomeManager.Application/`

| Componente | Estado | Notas |
|---|---|---|
| `DTOs/**` (Category, Product, Storage, Inventory) | ✅ | Cada entidad tiene `XDto`, `CreateXDto`, `UpdateXDto`. Inputs usan `required`; outputs usan `= string.Empty`. Los DTOs **no** exponen `IsDeleted`/auditoría. |
| `Interfaces/IBaseService.cs` | ✅ | Genérico `<TDto, TCreateDto, TUpdateDto>`: `GetAllAsync`, `GetByIdAsync`, `CreateAsync`, `UpdateAsync`, `DeleteAsync`. |
| `Interfaces/INamedEntityService.cs` | ✅ | Extiende `IBaseService` + `GetByNameAsync(string)`. Para entidades con nombre. |
| `Interfaces/ICategoryService.cs` | ✅ | `: INamedEntityService<...>` (vacía, lista para métodos propios). |
| `Interfaces/IStorageService.cs` | ✅ | `: INamedEntityService<...>` (vacía). |
| `Interfaces/IProductService.cs` | ✅ | `: INamedEntityService<...>` + `GetByBrandAsync`, `GetByPriceRangeAsync`, `GetByCategoryAsync`. |
| `Interfaces/IInventoryService.cs` | ✅ | `: IBaseService<...>` (no named) + `GetByProductIdAsync`, `GetByStorageIdAsync`, `GetExpirationDateAsync`. |
| `Services/CategoryService.cs` | ❌ | Declara `: ICategoryService` pero **sin métodos** → rompe el build. |
| `Services/ProductService.cs` | ❌ | Igual. |
| `Services/StorageService.cs` | ❌ | Igual. |
| `Services/InventoryService.cs` | ❌ | Igual. |
| `Mappings/MappingProfile.cs` | ⬜ | Scaffold vacío (Profile sin mapeos). |
| `Validators/**` | ⬜ | Scaffold vacío (AbstractValidator sin reglas). |
| `DependencyInjection.cs` | ⬜ | `AddApplication()` retorna services sin registrar nada todavía. |

### 3. Infrastructure — ⬜ SCAFFOLD VACÍO

`src/HomeManager.Infrastructure/` — `HomeManagerDbContext` (sin `DbSet`), 4 `IEntityTypeConfiguration` vacías, `Repository<T>` y `UnitOfWork` con constructor pero sin implementar interfaces, `AddInfrastructure(IConfiguration)` sin registros.

### 4. API — ⬜ SCAFFOLD VACÍO

`src/HomeManager.API/` — 4 controllers vacíos (`[ApiController]` + ruta, sin endpoints), `Program.cs` con Swagger + `AddApplication()` + `AddInfrastructure()` ya cableados, `appsettings.json` con `ConnectionStrings:DefaultConnection` apuntando a `localhost`.

### Tests — ⬜ ESTRUCTURA VACÍA

`tests/HomeManager.Tests/` con carpetas espejo y `.gitkeep`. Sin tests.

---

## Próximo paso exacto

Implementar los 4 servicios. Empezar por `src/HomeManager.Application/Services/CategoryService.cs`:

1. Inyectar dependencias por constructor: `IRepository<Category>`, `IUnitOfWork`, `IMapper`.
2. Implementar los métodos de `ICategoryService` (heredados de `INamedEntityService` + `IBaseService`).
3. Patrón de cada método: validar → mapear DTO↔entidad → llamar repo → `SaveChangesAsync` → devolver DTO.

> Recordatorio de método didáctico: el tutor **no** escribe el código. Puede dar la firma/esqueleto; el alumno rellena el cuerpo. Si hay error, el tutor señala la línea y pregunta.

Cuando los 4 servicios compilen, seguir con: `MappingProfile` → `Validators` → `DependencyInjection.AddApplication()` → luego capa Infrastructure (DbContext + Repository real + UnitOfWork) → luego API (endpoints en controllers) → primera migración EF.

---

## Decisiones de diseño tomadas (y por qué)

- **`Guid` como Id** (no `int`): unicidad global, evita colisiones al sincronizar entre sistemas. Trade-off aceptado: fragmentación de índices en SQL Server.
- **`BaseEntity` es `abstract`**: el compilador prohíbe instanciarla directamente; toda entidad debe heredar.
- **Categoría es entidad, no enum**: las categorías deben poder crearse/editarse en runtime por el usuario, no estar fijadas en compilación.
- **FK + navegación juntas** (`CategoryId` + `Category?`): permite asignar por Id sin cargar el objeto, y navegar (`p.Category.Name`) cuando se hace `Include`.
- **Relación unidireccional**: `Product` conoce su `Category`, pero `Category` no tiene `ICollection<Product>` porque ningún caso de uso lo exige todavía. La relación se configurará en `CategoryConfiguration`.
- **`IRepository` e `IUnitOfWork` viven en Domain**: la interfaz pertenece a quien la usa (Application), no a quien la implementa (Infrastructure). Mantener la flecha de dependencia apuntando hacia adentro (DIP).
- **`Remove`/`EditById` son `void`; los demás son `Task`**: si el método solo marca estado en memoria de EF, es `void`; si toca la BD, es `Task`. Regla práctica: seguimos la firma que EF Core expone (`AddAsync` async, `Remove`/`Update` sync).
- **DTOs separados de entidades**: el DTO es el contrato público (sin `IsDeleted` ni auditoría); la entidad es el contrato interno. Evolucionan independientemente.
- **`Create*Dto` usa `required`; `*Dto` de salida usa `= string.Empty`**: en creación el campo es obligatorio (lo fuerza el compilador); en salida solo evita warning de nullabilidad.
- **Jerarquía de interfaces `IBaseService` → `INamedEntityService` → `IXService`**: DRY para el CRUD común, `INamedEntityService` agrega `GetByNameAsync` (Category, Storage), y cada `IXService` agrega lo suyo. El controller depende de `IXService`, no de `IBaseService`, para no atarse al contrato genérico.
- **Sufijo `Async` obligatorio** en todo método que retorna `Task` (convención .NET).
- **Retornar `IEnumerable<T>` en vez de `List<T>`**: dar al caller el contrato mínimo (solo iterar), no capacidad de mutar la colección.

---

## Pregunta en "parking lot" (pendiente para fase de controllers)

- ¿Endpoint `GetByName` dedicado vs. filtro por query string? — decidir al diseñar los controllers.

---

## Comandos útiles

```bash
# Build de toda la solución
dotnet build

# Build solo de Application (la capa en progreso)
dotnet build src/HomeManager.Application

# Levantar la API + Swagger (cuando haya endpoints)
dotnet run --project src/HomeManager.API --launch-profile http
# Swagger: http://localhost:5187/swagger

# Tests
dotnet test
```

---

## Git

- Rama: `main`. Working tree limpio al momento de escribir este documento.
- Connection string por defecto en `appsettings.json`:
  `Server=localhost;Database=HomeManager;Trusted_Connection=True;TrustServerCertificate=True;`
  Ajustar según la instancia SQL Server del equipo donde se retome.
- `.gitignore` ya ignora `.vs/`, `*.user`, `.idea/`, `.vscode/`, `bin/`, `obj/`.
