# HomeManager-API — Clean Architecture Reference Guide

> This is not a tutorial. It is a reference for when you ask yourself *"why does this live here and not there?"* — read the section you need, skip the rest.

---

## Dependency Rule (the only rule that matters)

```
HomeManager.Domain
       ▲
HomeManager.Application
       ▲
HomeManager.Infrastructure   HomeManager.API
             ▲_____________________▲
```

**Source code dependencies point inward only.** Domain knows nothing. Application knows Domain. Infrastructure knows both. API knows Application and Infrastructure — but only to wire DI, never to call Infrastructure directly from a controller.

---

## 1. Domain

### The one-sentence rule
Domain is allowed to describe *what* the business is; it is forbidden to know *how* data is stored, *how* requests arrive, or *which* framework is running.

### The pear-and-apple analogy
Imagine a botanist writing the official scientific description of an apple: its shape, seeds, skin, how it ripens. The botanist doesn't care whether the apple ends up in a supermarket or a juice factory. She certainly doesn't write "apple should be stored in an SQL table with a primary key of type `int`." Domain is the botanist's notebook — pure description, zero logistics.

### What lives here and why

| Component | File(s) | Why here and not elsewhere |
|---|---|---|
| **BaseEntity** | `src/HomeManager.Domain/Common/BaseEntity.cs` | Every entity shares identity fields (`Id`). Centralising them here means Application and Infrastructure can rely on a guaranteed shape without coupling to a specific ORM. |
| **Entities** | `src/HomeManager.Domain/Entities/` | `Category`, `Product`, `Storage`, `Inventory` are the vocabulary of the problem. They must exist independently of how they are persisted or transported. |
| **IRepository\<T\>** | `src/HomeManager.Domain/Interfaces/IRepository.cs` | The *contract* for persistence lives here so Domain can describe the operation ("I need to find a Category") without depending on EF Core. The implementation lives in Infrastructure. |
| **IUnitOfWork** | `src/HomeManager.Domain/Interfaces/IUnitOfWork.cs` | Same reason: the concept of "commit a transaction" belongs to the business, not to any particular database driver. |

### Code example — the minimum honest version

```csharp
// src/HomeManager.Domain/Common/BaseEntity.cs
namespace HomeManager.Domain.Common;

public abstract class BaseEntity
{
    // Id lives here so every entity is guaranteed to have identity.
    // Infrastructure (EF Core) will read this property via reflection —
    // but Domain doesn't reference EF at all. That's the point.
    public int Id { get; set; }
}

// src/HomeManager.Domain/Entities/Category.cs
namespace HomeManager.Domain.Entities;

public class Category : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    // Navigation property: fine in Domain because it describes a real
    // business relationship — a Category contains Products.
    public ICollection<Product> Products { get; set; } = [];

    // ❌ TEMPTING MISTAKE — do NOT do this:
    // public string ToJson() => JsonSerializer.Serialize(this);
    //
    // Serialisation is a transport concern. The moment Category knows about
    // JSON it has taken a dependency on how data leaves the system.
    // Domain doesn't care about transport. Ever.
}

// src/HomeManager.Domain/Interfaces/IRepository.cs
namespace HomeManager.Domain.Interfaces;

// The constraint "where T : BaseEntity" is intentional:
// it guarantees every entity has an Id, so AddAsync / GetByIdAsync
// make sense without a generic parameter explosion.
public interface IRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    Task AddAsync(T entity);
    void Update(T entity);
    void Delete(T entity);
}
```

### The "why not just..." challenge

**"Why define `IRepository` in Domain instead of Infrastructure, where the implementation actually lives?"**

Because if `IRepository<T>` lived in Infrastructure, Application would have to reference Infrastructure to use it — and that wire would mean your business logic depends on EF Core. Tomorrow you swap EF for Dapper. Now you're rewriting Application. The interface belongs to the layer that *uses* it, not the layer that *implements* it. Domain declares the need; Infrastructure fulfils it.

---

## 2. Application

### The one-sentence rule
Application is allowed to orchestrate domain objects and define use-case logic; it is forbidden to know about HTTP, databases, file systems, or any specific framework.

### The pear-and-apple analogy
The Application layer is the head chef. She receives an order slip ("create a new category called Cleaning"), decides which ingredients to use (Domain entities), validates that the order makes sense (FluentValidation), and sends the finished plate to the waiter (API). She doesn't grow the fruit herself, and she doesn't set the table. She also refuses to talk to the dishwasher (Infrastructure) directly — she calls out "I need a clean pan" and one appears. She doesn't know if it came from the rack or the machine.

### What lives here and why

| Component | File(s) | Why here and not elsewhere |
|---|---|---|
| **DTOs** | `src/HomeManager.Application/DTOs/` | Data that crosses the API boundary should not be a raw Entity — callers don't need (and shouldn't see) navigation properties, internal state, or persistence artefacts. DTOs are purpose-built shapes for each operation. |
| **Service Interfaces** | `src/HomeManager.Application/Interfaces/` | `ICategoryService`, `IProductService`, etc. declare *what* use cases exist. The API depends on these interfaces, never on the concrete `CategoryService`. That's what makes the controller testable. |
| **Services** | `src/HomeManager.Application/Services/` | `CategoryService` implements the use case: validate, map, persist, return. This is the only place where business rules (beyond entity invariants) live. |
| **MappingProfile** | `src/HomeManager.Application/Mappings/MappingProfile.cs` | AutoMapper configuration belongs here because it defines how Domain objects become DTOs — a transformation concern, not a persistence or HTTP concern. |
| **Validators** | `src/HomeManager.Application/Validators/` | Input validation is a use-case concern: "can this operation proceed?" It belongs in Application, not in the controller (which should be thin) and not in the entity (which should be free of framework dependencies). |

### Code example — the minimum honest version

```csharp
// src/HomeManager.Application/Interfaces/ICategoryService.cs
namespace HomeManager.Application.Interfaces;

// The controller depends on this interface, not on CategoryService directly.
// This makes swapping implementations (e.g. for tests) trivial.
public interface ICategoryService
{
    Task<IEnumerable<CategoryDto>> GetAllAsync();
    Task<CategoryDto?> GetByIdAsync(int id);
    Task<CategoryDto> CreateAsync(CreateCategoryDto dto);
    Task<CategoryDto?> UpdateAsync(int id, UpdateCategoryDto dto);
    Task<bool> DeleteAsync(int id);
}

// src/HomeManager.Application/Services/CategoryService.cs
// (fragment — showing the Create path only)
public class CategoryService : ICategoryService
{
    private readonly IRepository<Category> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IValidator<CreateCategoryDto> _validator;

    // Dependencies are all interfaces — none of them reference EF, SQL, or HTTP.
    // This class has no idea how categories are stored. That's correct.
    public CategoryService(
        IRepository<Category> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IValidator<CreateCategoryDto> validator)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _mapper    = mapper;
        _validator = validator;
    }

    public async Task<CategoryDto> CreateAsync(CreateCategoryDto dto)
    {
        // Validate first — fail fast before touching the database.
        var result = await _validator.ValidateAsync(dto);
        if (!result.IsValid)
            throw new ValidationException(result.Errors);

        // Map DTO → Entity. AutoMapper reads the MappingProfile defined in
        // Application — no EF types involved.
        var category = _mapper.Map<Category>(dto);

        await _repository.AddAsync(category);

        // UnitOfWork commits the transaction. Service doesn't call DbContext directly.
        await _unitOfWork.SaveChangesAsync();

        // Return a DTO, never the raw Entity.
        // Returning the Entity would expose navigation properties the caller
        // might serialise, creating an accidental contract with your DB schema.
        return _mapper.Map<CategoryDto>(category);

        // ❌ TEMPTING MISTAKE — do NOT do this:
        // return category;
        //
        // Returning the Entity from a service leaks Domain internals
        // into the API response. Now your JSON output shape is tied to your
        // Entity, and any EF lazy-load can trigger a SELECT mid-serialisation.
    }
}

// src/HomeManager.Application/Validators/Category/CreateCategoryValidator.cs
public class CreateCategoryValidator : AbstractValidator<CreateCategoryDto>
{
    public CreateCategoryValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(100);

        RuleFor(x => x.Description)
            .MaximumLength(500).When(x => x.Description is not null);
    }
}
```

### The "why not just..." challenge

**"Why do we need DTOs if we already have entities?"**

Because your Entity and your API contract are two different things that happen to look similar today. `Category` has a `Products` navigation property — serialize it and you've just dumped the entire product catalogue into a category response. DTOs let you define exactly what each operation exposes. `CreateCategoryDto` has no `Id` (the client can't set it). `CategoryDto` has no navigation properties (you return them on demand). Entities and DTOs evolve independently, and that independence is the point.

---

## 3. Infrastructure

### The one-sentence rule
Infrastructure is allowed to know about databases, file systems, external APIs, and frameworks; it is forbidden to contain any business logic or decide anything about the domain.

### The pear-and-apple analogy
Infrastructure is the warehouse. The head chef (Application) shouts "I need five apples stored in bin #7." The warehouse worker doesn't question whether apple pie is a good idea — that's the chef's problem. The worker just knows *where things are physically kept* and *how to retrieve them*. If you switch from wooden bins to refrigerated shelves, the chef doesn't rewrite her recipes. Only the warehouse worker learns the new layout.

### What lives here and why

| Component | File(s) | Why here and not elsewhere |
|---|---|---|
| **HomeManagerDbContext** | `src/HomeManager.Infrastructure/Persistence/HomeManagerDbContext.cs` | EF Core's `DbContext` is a framework type. It belongs in the layer that is *allowed* to know about frameworks. Domain and Application reference only interfaces. |
| **IEntityTypeConfiguration\<T\>** | `src/HomeManager.Infrastructure/Persistence/Configurations/` | Table names, column types, foreign keys, and indexes are persistence details. They live here so that changing a column type doesn't touch Domain or Application. |
| **Repository\<T\>** | `src/HomeManager.Infrastructure/Repositories/Repository.cs` | Concrete implementation of `IRepository<T>`. EF Core calls live here and nowhere else. |
| **UnitOfWork** | `src/HomeManager.Infrastructure/Repositories/UnitOfWork.cs` | Wraps `DbContext.SaveChangesAsync()`. Application calls `IUnitOfWork.SaveChangesAsync()` without knowing a `DbContext` exists. |
| **DependencyInjection** | `src/HomeManager.Infrastructure/DependencyInjection.cs` | Registers EF Core and repositories. API calls `AddInfrastructure()` — one method, zero EF knowledge leaking into the API project. |

### Code example — the minimum honest version

```csharp
// src/HomeManager.Infrastructure/Repositories/Repository.cs
// This class is the only place in the entire solution that touches DbSet<T>.
public class Repository<T> : IRepository<T> where T : BaseEntity
{
    protected readonly HomeManagerDbContext _context;
    // DbSet<T> is the EF handle to a table. It lives here — never in Domain or Application.
    private readonly DbSet<T> _dbSet;

    public Repository(HomeManagerDbContext context)
    {
        _context = context;
        _dbSet   = context.Set<T>();
    }

    public async Task<T?> GetByIdAsync(int id)
        => await _dbSet.FindAsync(id);

    public async Task<IEnumerable<T>> GetAllAsync()
        => await _dbSet.ToListAsync();

    public async Task AddAsync(T entity)
        => await _dbSet.AddAsync(entity);

    public void Update(T entity)
        => _dbSet.Update(entity);

    public void Delete(T entity)
        => _dbSet.Remove(entity);

    // ❌ TEMPTING MISTAKE — do NOT do this:
    // public async Task<IEnumerable<Category>> GetCategoriesWithProducts()
    //     => await _context.Categories.Include(c => c.Products).ToListAsync();
    //
    // Repository<T> is generic for a reason. Domain-specific queries that bypass
    // the interface contract belong in a dedicated CategoryRepository : Repository<Category>
    // with its own interface. Stuffing EF-specific queries into the base breaks
    // the Open/Closed Principle: every new query forces you to modify Repository<T>.
}

// src/HomeManager.Infrastructure/Persistence/Configurations/CategoryConfiguration.cs
public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        // Table mapping is an infrastructure decision. The Category entity
        // has no [Table], [Column], or [MaxLength] attributes — keeping it
        // free of EF annotations is what lets Domain stay framework-agnostic.
        builder.ToTable("Categories");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
               .IsRequired()
               .HasMaxLength(100);

        builder.Property(c => c.Description)
               .HasMaxLength(500);

        // Relationship: one Category → many Products.
        // Defined here, not on the entity. The entity just has a navigation property.
        builder.HasMany(c => c.Products)
               .WithOne(p => p.Category)
               .HasForeignKey(p => p.CategoryId);
    }
}

// src/HomeManager.Infrastructure/DependencyInjection.cs
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<HomeManagerDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection")));

        // IRepository<T> → Repository<T>: the interface is from Domain,
        // the implementation is from Infrastructure.
        // This is the only file in the solution that knows both exist.
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}
```

### The "why not just..." challenge

**"Why not call `DbContext` directly inside `CategoryService`?"**

Because `CategoryService` would then have to reference `Microsoft.EntityFrameworkCore` — an Infrastructure package — making Application depend on Infrastructure. The dependency arrow would point outward, breaking Clean Architecture. More practically: you can no longer test `CategoryService` without spinning up a real (or in-memory) database. With `IRepository<Category>` you mock the interface in two lines. With `DbContext` you're writing test database setup code forever.

---

## 4. API

### The one-sentence rule
API is allowed to receive HTTP requests, call Application services, and return HTTP responses; it is forbidden to contain business logic, call Infrastructure directly, or construct domain objects.

### The pear-and-apple analogy
The API layer is the waiter. He takes the customer's order (HTTP request), hands it to the chef (Application), and brings the plate back (HTTP response). A good waiter does not walk into the kitchen and start chopping vegetables. He definitely does not go to the warehouse to fetch ingredients himself. His job is translation: convert what the customer says into something the kitchen understands, and convert what the kitchen returns into something the customer can eat.

### What lives here and why

| Component | File(s) | Why here and not elsewhere |
|---|---|---|
| **Controllers** | `src/HomeManager.API/Controllers/` | HTTP is a delivery mechanism. Controllers translate HTTP verbs and status codes. They know nothing about SQL or business rules — only about the contract between client and service. |
| **Program.cs** | `src/HomeManager.API/Program.cs` | Application composition root. The only place allowed to wire `AddApplication()` and `AddInfrastructure()` together. Infrastructure is referenced here only for registration — controllers never import it. |
| **appsettings.json** | `src/HomeManager.API/appsettings.json` | Configuration (connection strings, etc.) is an environment concern. It lives at the entry point of the application, not buried in Infrastructure or Domain. |

### Code example — the minimum honest version

```csharp
// src/HomeManager.API/Controllers/CategoriesController.cs
[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryService _service;

    // The controller depends on the interface from Application — not on
    // CategoryService directly, and certainly not on Repository<Category>.
    public CategoriesController(ICategoryService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CategoryDto>>> GetAll()
    {
        var categories = await _service.GetAllAsync();
        return Ok(categories);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CategoryDto>> GetById(int id)
    {
        var category = await _service.GetByIdAsync(id);
        return category is null ? NotFound() : Ok(category);
    }

    [HttpPost]
    public async Task<ActionResult<CategoryDto>> Create(CreateCategoryDto dto)
    {
        // Controller passes the DTO it received to the service.
        // It does NOT map the DTO to an entity here — that's Application's job.
        var created = await _service.CreateAsync(dto);

        // 201 Created with Location header pointing to the new resource.
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);

        // ❌ TEMPTING MISTAKE — do NOT do this:
        // var category = new Category { Name = dto.Name }; // ← constructing an entity in a controller
        // _context.Categories.Add(category);               // ← calling DbContext from a controller
        // await _context.SaveChangesAsync();
        //
        // This collapses four layers into one method. You've just made
        // the controller responsible for mapping, persistence, AND HTTP.
        // Good luck unit-testing that without a running database.
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<CategoryDto>> Update(int id, UpdateCategoryDto dto)
    {
        var updated = await _service.UpdateAsync(id, dto);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _service.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }
}

// src/HomeManager.API/Program.cs
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// These two lines are the only place where Infrastructure and Application
// are wired together. If you need to swap Infrastructure for a different
// database tomorrow, you change AddInfrastructure() — nothing else.
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
```

### The "why not just..." challenge

**"Why not put the business logic directly in the controller?"**

You can. It compiles. It runs. And then six months later you need to expose the same logic via a background job, a CLI tool, or a gRPC endpoint — and you realise the logic is trapped inside `ActionResult` return types and `ControllerBase` method calls. You end up copy-pasting. Put the logic in `CategoryService`, and the controller is just a thin HTTP adapter. The background job calls the same service. The CLI calls the same service. Nothing is copy-pasted.

---

## Cross-Layer Interaction Map

### Lifecycle: `POST /api/categories` — user creates a new Category

```
HTTP POST /api/categories  { "name": "Cleaning", "description": "..." }
         │
         ▼
[1] CategoriesController.Create(CreateCategoryDto dto)
         │
         ▼
[2] ICategoryService.CreateAsync(CreateCategoryDto dto)
         │  (resolved to CategoryService at runtime via DI)
         ▼
[3] CreateCategoryValidator.ValidateAsync(dto)
         │
         ▼
[4] IMapper.Map<Category>(dto)
         │
         ▼
[5] IRepository<Category>.AddAsync(category)
         │  (resolved to Repository<Category> at runtime)
         ▼
[6] HomeManagerDbContext.Set<Category>().AddAsync(category)
         │
         ▼
[7] IUnitOfWork.SaveChangesAsync()
         │  (resolved to UnitOfWork → DbContext.SaveChangesAsync())
         ▼
[8] IMapper.Map<CategoryDto>(category)
         │
         ▼
[9] CreatedAtAction(201) with CategoryDto body
```

---

### What crosses each boundary and why

| Step | What is passed | Why that type |
|---|---|---|
| **[1] HTTP → Controller** | `CreateCategoryDto` (deserialized by ASP.NET model binding) | A DTO, not an entity. The client never sends an `Id` (it doesn't exist yet) and shouldn't send navigation properties. The DTO shape is owned by Application, not by the HTTP layer. |
| **[2] Controller → Service** | `CreateCategoryDto` | The controller forwards exactly what it received. It does not transform the data — transformation is Application's responsibility. |
| **[3] Validate** | `CreateCategoryDto` | Validation runs on the input shape, before any domain object is constructed. Failing here is cheap — no entity was created, no DB call was made. |
| **[4] Map DTO → Entity** | `Category` (domain entity) | Only after validation succeeds is a `Category` created. AutoMapper uses `MappingProfile` defined in Application — the controller and the repository never see this step. |
| **[5] Service → Repository** | `Category` (domain entity) | The repository contract (`IRepository<Category>`) is defined in Domain, so passing a `Category` entity is exactly right. The service doesn't pass a DTO here — the repository's job is persistence, not deserialization. |
| **[6] Repository → DbContext** | `Category` entity via `DbSet<Category>` | EF Core operates on entities, not DTOs. The entity arrives in Infrastructure still without an `Id` — EF will assign it after `SaveChanges`. |
| **[7] UnitOfWork.SaveChangesAsync()** | — (no argument) | The UnitOfWork pattern separates *declaring intent* (AddAsync) from *committing* (SaveChangesAsync). This allows multiple operations to be batched in one transaction before anything touches the DB. |
| **[8] Map Entity → DTO** | `CategoryDto` | After save, EF has populated `category.Id`. The service maps back to a DTO before returning — the controller receives a `CategoryDto`, never the raw entity. |
| **[9] Controller → HTTP** | `CategoryDto` as JSON body | ASP.NET serializes the DTO. The HTTP client sees a clean, intentional contract — not an Entity with circular navigation properties that would trigger a `JsonException`. |

---

### What would break if you skipped a layer

| Skipped step | What you'd do instead | What breaks |
|---|---|---|
| Skip **Controller → Service** | Controller calls `Repository<Category>` directly | No validation, no mapping, no business logic. The controller is now responsible for all three. You can never call this logic from a background job or CLI without duplicating it. |
| Skip **Validation** | Map and persist immediately | Invalid data reaches the database. You're now enforcing constraints only at the DB level, which means SQL exceptions instead of readable error messages. |
| Skip **DTO → Entity mapping** | Pass `CreateCategoryDto` directly to `Repository<T>` | `IRepository<T>` requires `T : BaseEntity`. A DTO is not a `BaseEntity`. It doesn't compile. If you worked around it, the repository would persist a DTO shape — which has no DB configuration. |
| Skip **Repository** | `CategoryService` calls `HomeManagerDbContext` directly | Application now references Infrastructure. The dependency arrow reverses. You can no longer test `CategoryService` without EF Core running. |
| Skip **UnitOfWork** | Call `DbContext.SaveChangesAsync()` in the Repository | You lose transaction control. Two repositories participating in one use case can no longer commit atomically — one might save while the other fails, leaving the DB in a partial state. |
| Skip **Entity → DTO (return)** | Return the `Category` entity from the service | ASP.NET's JSON serializer follows navigation properties. `Category.Products` triggers a lazy-load SELECT for every category in the response. Your API has just become accidentally expensive. |
