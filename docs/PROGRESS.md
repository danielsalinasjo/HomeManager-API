# HomeManager-API — Estado de avance

> Documento de continuidad para retomar el trabajo desde cualquier equipo.
> Última actualización: 2026-07-08

---

## TL;DR — dónde estamos

- **Arquitectura:** Clean Architecture, 4 capas, .NET 10, EF Core 10 + SQL Server, AutoMapper, FluentValidation, Swagger.
- **Capa Domain:** ✅ completa (entidades con propiedades + interfaces de contrato).
- **Capa Application:** 🚧 en progreso — DTOs ✅, interfaces de servicio ✅, **`CategoryService` completo** ✅ (6 métodos), los otros 3 servicios pendientes.
- **Capas Infrastructure y API:** scaffolding vacío (clases creadas sin lógica).
- ⚠️ **El build sigue ROTO a propósito** por dos razones: (a) `ProductService`/`StorageService`/`InventoryService` siguen vacíos; (b) queda **un ajuste pendiente en `CategoryService`**: las líneas 10 y 12 usan `INamedEntityRepository` sin argumento de tipo — hay que cambiarlo a `INamedEntityRepository<Category>` ahora que la interfaz es genérica.

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
| `Interfaces/INamedEntityRepository.cs` | ✅ | `INamedEntityRepository<T> : IRepository<T> where T : BaseEntity` + `GetByNameAsync(string)`. Abstracción reutilizable para entidades con nombre (Category, Storage). ⚠️ Falta la interfaz marcadora `INamedEntity` para poder implementarla genéricamente (ver "parking lot"). |
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
| `Services/CategoryService.cs` | ✅ | Los 6 métodos implementados a mano (sin AutoMapper todavía). Falta solo el ajuste `INamedEntityRepository<Category>` en el constructor (líneas 10/12). |
| `Services/ProductService.cs` | ❌ | Vacío → rompe el build. |
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

1. **Ajuste inmediato:** en `CategoryService` cambiar `INamedEntityRepository` → `INamedEntityRepository<Category>` (líneas 10 y 12). Con eso `CategoryService` compila.
2. **Decisión pendiente antes de implementar el repo:** crear la interfaz marcadora `INamedEntity { string Name { get; } }` en Domain, hacer que `Category`/`Storage`/`Product` la implementen, y reforzar la restricción de `INamedEntityRepository<T>` a `where T : BaseEntity, INamedEntity`. Sin eso, el `GetByNameAsync` genérico no podrá hacer `.Where(e => e.Name == name)` (BaseEntity no tiene `Name`).
3. **Implementar los 3 servicios restantes** (`Product`, `Storage`, `Inventory`) siguiendo el patrón ya establecido en `CategoryService`: validar → construir entidad (solo campos propios) → repo → `SaveChangesAsync` → devolver DTO. Ojo: `Product`/`Storage` usan la variante con nombre; `Inventory` no.

> El mapeo DTO↔entidad se hace **a mano** por ahora (para entender qué hace). AutoMapper (`IMapper` + `MappingProfile`) se introducirá **después**, como refactor DRY una vez que se sienta la repetición entre los 4 servicios.

> Recordatorio de método didáctico: el tutor **no** escribe el código. Puede dar la firma/esqueleto; el alumno rellena el cuerpo. Si hay error, el tutor señala la línea y pregunta.

Cuando los 4 servicios compilen, seguir con: `AutoMapper`/`MappingProfile` → `Validators` (FluentValidation) → `DependencyInjection.AddApplication()` → luego capa Infrastructure (DbContext + Repository real + UnitOfWork) → luego API (endpoints en controllers) → primera migración EF.

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
- **Repositorio dedicado para consultas no-por-Id** (`INamedEntityRepository<T> : IRepository<T>`): el genérico `IRepository<T>` se mantiene limpio (OCP); las búsquedas específicas (`GetByNameAsync`) viven en una interfaz aparte. Evita contaminar el repositorio base cada vez que aparece una consulta nueva.
- **Update con entidad rastreada (sin `EditById`)**: el patrón es `GetByIdAsync` → modificar la entidad → `SaveChangesAsync`. EF Core detecta el cambio vía change tracker y genera un `UPDATE` solo de lo modificado. Requisito: cuando se implemente el repo, `GetByIdAsync` **no** debe usar `AsNoTracking()`. `EditById` queda como herramienta para el caso "desconectado" (entidad no traída del contexto).
- **`Select` en vez de bucle + `Append`/`Add`** para mapear colección entidad→DTO: `Select` es una *proyección* declarativa ("transforma cada elemento"); reemplaza al `foreach` entero. `Append` agrega un solo elemento y encadenarlo en un loop crea N envoltorios diferidos.
- **Auditoría (`CreatedAt`/`LastUpdatedAt`/`IsDeleted`) NO se llena en los servicios**: es responsabilidad del `DbContext` en Infrastructure (una sola vez, aplica a las 4 entidades). El servicio solo toca los campos propios del DTO.
- **Restricciones de genéricos no se heredan**: `INamedEntityRepository<T>` debe repetir `where T : BaseEntity` porque su base `IRepository<T>` lo exige; `INamedEntityService<...>` no necesita restricción porque su base `IBaseService<...>` no impone ninguna.

---

## Preguntas en "parking lot"

- **Soft-delete vs. hard-delete** (decidir pronto): `CategoryService.DeleteAsync` usa `Remove` (borrado físico), pero `BaseEntity` tiene `IsDeleted`. Si se quiere soft-delete, el método debe parecerse a `UpdateAsync` (marcar `IsDeleted = true` + `SaveChanges`) en vez de usar `Remove`.
- **Interfaz marcadora `INamedEntity`** (bloquea la implementación del repo genérico): necesaria para que `GetByNameAsync` genérico pueda acceder a `.Name`. Ver "Próximo paso" #2.
- ¿Endpoint `GetByName` dedicado vs. filtro por query string? — decidir al diseñar los controllers.
- Pasar el `CancellationToken` real a `SaveChangesAsync` (hoy se usa `default`) — decidir al cablear los controllers.

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
