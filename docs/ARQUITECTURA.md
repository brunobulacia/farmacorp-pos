# Arquitectura y Pipeline de Comunicación — FarmacorpPOS

Documento que explica cómo fluye una operación **desde el menú de consola hasta la base de datos**, archivo por archivo, y por qué está organizado así.

---

## 1. Visión general (N-Capas)

El proyecto son 4 ensamblados (`.csproj`) con dependencias en una sola dirección. Las capas externas conocen a las internas, nunca al revés:

```
┌─────────────────────────────────────────────────────────────┐
│  FarmacorpPOS.Console        (Presentación / arranque)        │
│  Program.cs · Menu.cs                                         │
└───────────────┬─────────────────────────────────────────────┘
                │ referencia
                ▼
┌─────────────────────────────────────────────────────────────┐
│  FarmacorpPOS.Application     (Lógica de negocio)             │
│  ProductoService · VentaService · Reglas (Strategy)          │
└───────────────┬─────────────────────────┬───────────────────┘
                │ referencia               │ referencia
                ▼                          ▼
┌──────────────────────────────┐   ┌──────────────────────────┐
│  FarmacorpPOS.Infrastructure │   │  FarmacorpPOS.Domain     │
│  DbContext · Repository<T>   │──▶│  Entidades (POCO)        │
│  UnitOfWork                  │   │  ExpProducto, ErpProducto…│
└───────────────┬──────────────┘   └──────────────────────────┘
                │ EF Core
                ▼
        ┌───────────────────┐
        │  SQL Server 2022  │  (Docker, puerto 1433)
        │  DB: FarmacorpPOS │
        └───────────────────┘
```

**Regla de oro:** las flechas apuntan "hacia adentro". `Domain` no depende de nadie; es el núcleo. Esto permite cambiar la base de datos o la UI sin tocar las entidades ni las reglas.

| Capa | Proyecto | Responsabilidad | No debe contener |
|------|----------|-----------------|------------------|
| Presentación | `.Console` | Leer input, mostrar output, armar el contenedor DI | Lógica de negocio ni SQL |
| Aplicación | `.Application` | Orquestar reglas y casos de uso | Detalles de EF/SQL ni `Console.WriteLine` |
| Infraestructura | `.Infrastructure` | Persistencia (EF Core, mapeo, repos) | Reglas de negocio |
| Dominio | `.Domain` | Entidades puras (POCO) | Dependencias de frameworks |

---

## 2. Quién instancia a quién (Inyección de Dependencias)

Todo el cableado vive en [Program.cs](../FarmacorpPOS.Console/Program.cs). Ahí se construye un `ServiceProvider` (contenedor IoC) que sabe fabricar cada objeto y resolver sus dependencias por constructor.

```csharp
services.AddDbContext<FarmacorpDbContext>(o => o.UseSqlServer(connectionString)); // Scoped
services.AddScoped<IUnitOfWork, UnitOfWork>();
services.AddSingleton<ReglasNegocioProvider>();   // estrategia persiste entre operaciones
services.AddScoped<ProductoService>();
services.AddScoped<VentaService>();
```

Cadena de resolución cuando se pide un `VentaService`:

```
VentaService
 ├─ IUnitOfWork ──▶ UnitOfWork
 │                   └─ FarmacorpDbContext ──▶ connectionString (appsettings.json)
 └─ ReglasNegocioProvider (singleton: Base ⇄ GanaMax)
```

- **Scoped**: una instancia por *scope*. [Menu.cs](../FarmacorpPOS.Console/Menu.cs) abre un `scope` nuevo por cada operación del menú → cada acción usa un `DbContext` fresco (sin estado colgado entre operaciones).
- **Singleton**: `ReglasNegocioProvider` es único en toda la app, por eso al cambiar de estrategia (opción 5) el cambio se mantiene en las siguientes operaciones.

`appsettings.json` aporta la cadena de conexión; nada está hardcodeado en el código de la app.

---

## 3. Pipeline completo: "Registrar una venta" paso a paso

Es el caso más rico porque aplica las 4 reglas de negocio. Seguimos el dato desde la tecla hasta la fila en SQL.

```
Usuario teclea "4"
      │
      ▼
[Console] Menu.RegistrarVentaAsync()                      ← lee idProducto, cliente, cantidad
      │   abre un scope DI y resuelve VentaService
      ▼
[Application] VentaService.RegistrarVentaAsync(id, cliente, cant)
      │   1. uow.Productos.GetByIdAsync(id)       ┐
      │   2. uow.ErpProductos.GetByIdAsync(id)    │ lecturas
      │   3. uow.ProductosCategorias.GetAllAsync()┘
      │   4. reglas.ValidarStock(erp, cant)        (RN4)  ── si falla: throw
      │   5. reglas.CalcularDescuento(prod, nCats) (RN3)
      │   6. Total = Precio*Cant*(1-Descuento)
      │   7. venta.UniqueProducto = erp.UniqueCodigo (RN2)
      │   8. erp.Stock -= cant ; uow.ErpProductos.Update(erp) (RN5)
      │   9. uow.Ventas.AddAsync(venta)
      │  10. uow.SaveChangesAsync()
      ▼
[Infrastructure] UnitOfWork.SaveChangesAsync()
      │   delega en…
      ▼
[Infrastructure] Repository<T> / FarmacorpDbContext
      │   EF Core traduce el grafo de cambios a SQL
      │   (UPDATE ErpProductos…  +  INSERT VentasExpress…) en UNA transacción
      ▼
[SQL Server]  COMMIT
```

### Detalle archivo por archivo

| # | Archivo | Qué hace |
|---|---------|----------|
| 1 | [Menu.cs](../FarmacorpPOS.Console/Menu.cs) → `RegistrarVentaAsync` | Captura input, crea scope DI, resuelve `VentaService`, muestra el resultado o el error |
| 2 | [VentaService.cs](../FarmacorpPOS.Application/Services/VentaService.cs) | Orquesta las reglas RN2–RN5 usando el `IUnitOfWork` y el `ReglasNegocioProvider`. **Acá vive el negocio.** |
| 3 | [ReglasNegocioProvider.cs](../FarmacorpPOS.Application/Reglas/ReglasNegocioProvider.cs) | Devuelve la estrategia activa (`ReglasBase` o `ReglasGanaMax`) |
| 4 | [IReglasNegocio.cs](../FarmacorpPOS.Application/Reglas/IReglasNegocio.cs) + Reglas | Calculan precio, descuento y validación de stock según la estrategia |
| 5 | [UnitOfWork.cs](../FarmacorpPOS.Infrastructure/UnitOfWork.cs) | Agrupa los repositorios y expone `SaveChangesAsync()` (una transacción) |
| 6 | [Repository.cs](../FarmacorpPOS.Infrastructure/Repositories/Repository.cs) | CRUD genérico sobre `DbSet<T>` (`FindAsync`, `ToListAsync`, `Add`, `Update`, `Remove`) |
| 7 | [FarmacorpDbContext.cs](../FarmacorpPOS.Infrastructure/FarmacorpDbContext.cs) | Mapea entidades ⇄ tablas con Fluent API y genera el SQL |
| 8 | [Entidades](../FarmacorpPOS.Domain/) | Objetos en memoria que viajan por todas las capas |

---

## 4. El "lenguaje común": las entidades del Domain

Las clases de [FarmacorpPOS.Domain](../FarmacorpPOS.Domain/) (`ExpProducto`, `ErpProducto`, `VentaExpress`, …) son **POCOs**: sin atributos de EF, sin dependencias. Son el formato de datos que cruza todas las capas:

- El `Repository` las **lee/escribe** en la base.
- El `Service` las **modifica** aplicando reglas.
- El `Menu` las **muestra**.

Como ninguna capa traduce a "su propio modelo", el objeto `ExpProducto` que crea el servicio es el mismo que EF persiste. Esto mantiene el flujo simple y sin duplicación.

---

## 5. Cómo se traduce a SQL (capa Infrastructure)

El mapeo objeto→tabla está 100% en Fluent API dentro de `OnModelCreating` de [FarmacorpDbContext.cs](../FarmacorpPOS.Infrastructure/FarmacorpDbContext.cs) (no se usan Data Annotations). Puntos clave:

- **Nombres de tabla** explícitos: `ExpProductos`, `ErpProductos`, `VentasExpress`, etc.
- **Relación 1‑a‑1 compartiendo PK/FK**: `ErpProducto.IdProducto` es a la vez PK y FK hacia `ExpProducto` (`HasForeignKey<ErpProducto>(x => x.IdProducto)` + `ValueGeneratedNever()`).
- **Índices únicos** sobre `UniqueCodigo` en `ErpProductos` y `CodigosBarras`.
- **Auto‑referencia** en `Categorias` (categoría padre/hijo).
- **`DeleteBehavior.Restrict`** donde un borrado en cascada rompería integridad (ej. ventas, tipo de producto).

Ese modelo es el que `dotnet ef migrations` convierte en la migración `InitialCreate`, y `dotnet ef database update` aplica como `CREATE TABLE …` reales en SQL Server.

### Conexión física
```
appsettings.json
  └─ "DefaultConnection": Server=localhost,1433;Database=FarmacorpPOS;User Id=sa;…
       └─ UseSqlServer(...) en Program.cs
            └─ FarmacorpDbContext abre el socket TCP 1433 → contenedor Docker `sqlserver`
```

> Hay además un [FarmacorpDbContextFactory.cs](../FarmacorpPOS.Infrastructure/FarmacorpDbContextFactory.cs) que **solo** usa la herramienta `dotnet ef` en tiempo de diseño (porque la app usa `ServiceCollection` y no Generic Host, EF no puede autodescubrir el contexto de otra forma).

---

## 6. Patrones aplicados — implementación y porqué

Resumen y luego el detalle con código de cada uno:

| Patrón | Dónde | Para qué |
|--------|-------|----------|
| **Repository** | `Repository<T>` / `IRepository<T>` | Aísla el acceso a datos; los servicios no ven EF directamente |
| **Unit of Work** | `UnitOfWork` | Agrupa varios cambios (UPDATE stock + INSERT venta) en **una sola transacción** |
| **Strategy** | `IReglasNegocio` + `ReglasBase`/`ReglasGanaMax` | Cambiar reglas de precio/descuento/stock en runtime sin tocar los servicios |
| **IoC / DI** | `ServiceCollection` en `Program.cs` | Desacoplar la creación de objetos de su uso; testeable y configurable |
| **Factory (design-time)** | `FarmacorpDbContextFactory` | Permitir a `dotnet ef` construir el `DbContext` para migraciones |

---

### 6.1 Repository (genérico)

**Implementación** — [IRepository.cs](../FarmacorpPOS.Infrastructure/Repositories/IRepository.cs) define el contrato; [Repository.cs](../FarmacorpPOS.Infrastructure/Repositories/Repository.cs) lo implementa una sola vez para *cualquier* entidad vía `DbSet<T>`:

```csharp
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    Task AddAsync(T entity);
    void Update(T entity);
    void Delete(T entity);
}

public class Repository<T> : IRepository<T> where T : class
{
    protected readonly FarmacorpDbContext _context;
    protected readonly DbSet<T> _set;

    public Repository(FarmacorpDbContext context) { _context = context; _set = context.Set<T>(); }

    public async Task<T?> GetByIdAsync(int id) => await _set.FindAsync(id);
    public async Task<IEnumerable<T>> GetAllAsync() => await _set.ToListAsync();
    public async Task AddAsync(T entity) => await _set.AddAsync(entity);
    public void Update(T entity) => _set.Update(entity);
    public void Delete(T entity) => _set.Remove(entity);
}
```

**Por qué así:** un único genérico `Repository<T>` evita escribir un repo casi idéntico por cada entidad (DRY). Al exponer una interfaz, la capa Application depende de la *abstracción* `IRepository<T>` y no de EF Core: si mañana se cambia el ORM, los servicios no se tocan. `GetByIdAsync` devuelve `T?` (nullable) para forzar al servicio a manejar el "no existe".

---

### 6.2 Unit of Work

**Implementación** — [UnitOfWork.cs](../FarmacorpPOS.Infrastructure/UnitOfWork.cs) agrupa todos los repositorios sobre **el mismo** `DbContext` y centraliza el guardado:

```csharp
public class UnitOfWork : IUnitOfWork
{
    private readonly FarmacorpDbContext _context;

    public IRepository<ExpProducto> Productos { get; }
    public IRepository<ErpProducto> ErpProductos { get; }
    public IRepository<VentaExpress> Ventas { get; }
    // … resto de repos

    public UnitOfWork(FarmacorpDbContext context)
    {
        _context = context;
        Productos    = new Repository<ExpProducto>(context);
        ErpProductos = new Repository<ErpProducto>(context);
        Ventas       = new Repository<VentaExpress>(context);
        // … todos comparten el MISMO context
    }

    public Task<int> SaveChangesAsync() => _context.SaveChangesAsync();
    public void Dispose() => _context.Dispose();
}
```

**Por qué así:** una venta toca dos tablas — `UPDATE ErpProductos` (descuenta stock, RN5) e `INSERT VentasExpress`. Como todos los repos comparten el mismo `DbContext`, EF rastrea ambos cambios y el único `SaveChangesAsync()` los confirma en **una transacción atómica**: o se guarda todo o nada. Sin UnitOfWork cada repo podría guardar por su cuenta y dejar la base inconsistente (stock descontado sin venta registrada, o al revés).

---

### 6.3 Strategy — *el patrón central del ejercicio*

**Implementación** — una interfaz [IReglasNegocio.cs](../FarmacorpPOS.Application/Reglas/IReglasNegocio.cs) y dos implementaciones intercambiables ([ReglasBase.cs](../FarmacorpPOS.Application/Reglas/ReglasBase.cs) / [ReglasGanaMax.cs](../FarmacorpPOS.Application/Reglas/ReglasGanaMax.cs)):

```csharp
public interface IReglasNegocio
{
    string Nombre { get; }
    decimal CalcularPrecio(decimal costo);
    decimal CalcularDescuento(ExpProducto producto, int cantidadCategorias);
    bool ValidarStock(ErpProducto erp, int cantidad);
}

public class ReglasBase : IReglasNegocio          // margen 50%, dto 30%
{
    public decimal CalcularPrecio(decimal costo) => costo * 1.50m;
    public decimal CalcularDescuento(ExpProducto p, int cats) => cats == 1 ? 0.30m : 0m;
    public bool ValidarStock(ErpProducto erp, int cant) => erp.Stock >= cant;
}

public class ReglasGanaMax : IReglasNegocio        // margen 80%, dto 10%, deja >10 uds
{
    public decimal CalcularPrecio(decimal costo) => costo * 1.80m;
    public decimal CalcularDescuento(ExpProducto p, int cats) => cats == 1 ? 0.10m : 0m;
    public bool ValidarStock(ErpProducto erp, int cant) => erp.Stock >= cant && (erp.Stock - cant) > 10;
}
```

El servicio **no conoce las fórmulas**, solo pide la estrategia activa y delega — [VentaService.cs](../FarmacorpPOS.Application/Services/VentaService.cs):

```csharp
var reglas = _reglas.Actual;                                   // estrategia vigente
if (!reglas.ValidarStock(erp, cantidad)) throw new ...;        // RN4
var descuento = reglas.CalcularDescuento(producto, nCategorias); // RN3
var total = (precio * cantidad) * (1 - descuento);
```

**Por qué así:** el requisito dice que las reglas cambian entre *Base* y *GanaMax* y deben poder alternarse **en cualquier momento**. Con Strategy, agregar/cambiar una política es crear/editar una clase que implementa la interfaz — sin tocar `VentaService` ni `ProductoService` (principio Abierto/Cerrado). La alternativa (un `if (estrategia == "GanaMax") …` repartido por los servicios) sería frágil y se rompería con cada regla nueva.

---

### 6.4 Cambio de estrategia en runtime (Provider sobre Strategy)

**Implementación** — [ReglasNegocioProvider.cs](../FarmacorpPOS.Application/Reglas/ReglasNegocioProvider.cs):

```csharp
public enum TipoEstrategia { Base, GanaMax }

public class ReglasNegocioProvider
{
    public IReglasNegocio Actual { get; private set; } = new ReglasBase();

    public void Cambiar(TipoEstrategia tipo) =>
        Actual = tipo switch
        {
            TipoEstrategia.GanaMax => new ReglasGanaMax(),
            _ => new ReglasBase()
        };
}
```

**Por qué así:** si se inyectara `IReglasNegocio` directo, el contenedor DI fijaría la implementación al construir cada servicio y no se podría cambiar sin reiniciar. El `Provider` se registra como **singleton** y los servicios leen `_reglas.Actual` *en cada operación*, de modo que la opción 5 del menú (`provider.Cambiar(...)`) afecta inmediatamente a todas las operaciones siguientes. Es la respuesta concreta al "alternar en cualquier momento".

---

### 6.5 IoC / Inyección de Dependencias

**Implementación** — todo el grafo se declara en [Program.cs](../FarmacorpPOS.Console/Program.cs):

```csharp
services.AddDbContext<FarmacorpDbContext>(o => o.UseSqlServer(connectionString)); // Scoped
services.AddScoped<IUnitOfWork, UnitOfWork>();
services.AddSingleton<ReglasNegocioProvider>();   // estado de estrategia compartido
services.AddScoped<ProductoService>();
services.AddScoped<VentaService>();
var provider = services.BuildServiceProvider();
```

Y los servicios reciben sus dependencias **por constructor**, sin instanciarlas ellos mismos:

```csharp
public VentaService(IUnitOfWork uow, ReglasNegocioProvider reglas) { _uow = uow; _reglas = reglas; }
```

**Por qué así:** cada clase declara *qué* necesita, no *cómo* se construye. Esto permite (a) intercambiar implementaciones por la interfaz, (b) testear con mocks de `IUnitOfWork`, y (c) controlar el ciclo de vida: `DbContext`/`UnitOfWork`/servicios son **Scoped** (uno por operación del menú → `DbContext` fresco, sin entidades colgadas), mientras `ReglasNegocioProvider` es **Singleton** para que la estrategia persista.

---

### 6.6 Factory en tiempo de diseño

**Implementación** — [FarmacorpDbContextFactory.cs](../FarmacorpPOS.Infrastructure/FarmacorpDbContextFactory.cs) implementa `IDesignTimeDbContextFactory<FarmacorpDbContext>`:

```csharp
public class FarmacorpDbContextFactory : IDesignTimeDbContextFactory<FarmacorpDbContext>
{
    public FarmacorpDbContext CreateDbContext(string[] args)
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(/* ruta al proyecto Console */)
            .AddJsonFile("appsettings.json", optional: true)
            .Build();
        var options = new DbContextOptionsBuilder<FarmacorpDbContext>()
            .UseSqlServer(config.GetConnectionString("DefaultConnection") ?? "…fallback…")
            .Options;
        return new FarmacorpDbContext(options);
    }
}
```

**Por qué así:** la app arranca con `ServiceCollection` "a mano" (no Generic Host), así que `dotnet ef migrations`/`database update` no tiene de dónde sacar un `DbContext` ya configurado. Esta factory le da una vía explícita, leyendo la **misma** cadena de conexión del `appsettings.json` para no duplicar configuración. Solo se usa en tiempo de diseño; la app en ejecución nunca la toca.

---

## 7. Modelo de datos (entidades y relaciones)

```
        ┌────────────────────┐
        │   TiposProducto    │
        │ PK IdTipoProducto  │
        │    Descripcion     │
        └─────────┬──────────┘
                  │ 1
                  │            N
                  ▼
        ┌────────────────────────┐         1   1   ┌────────────────────────┐
        │      ExpProductos      │◄───────────────►│      ErpProductos      │
        │ PK IdProducto          │  (1-a-1, misma  │ PK,FK IdProducto       │
        │    Nombre              │   PK/FK)        │    Costo               │
        │    Precio              │                 │    UniqueCodigo  ◄UQ►  │
        │    Activo              │                 │    FechaRegistro       │
        │    FechaVencimiento    │                 │    Stock               │
        │    Observaciones       │                 └───────────┬────────────┘
        │ FK IdTipoProducto      │                             │ 1
        └───┬──────────────┬─────┘                             │
            │ 1            │ 1                                  │ N
            │ N           │ N                                  ▼
            ▼              ▼                          ┌────────────────────┐
 ┌────────────────────┐  ┌────────────────────────┐  │    CodigosBarras   │
 │   VentasExpress    │  │  ProductosCategorias   │  │ PK IdCodigoBarra   │
 │ PK Id              │  │ PK IdDetalle           │  │    UniqueCodigo ◄UQ►│
 │    Fecha           │  │    FechaCreacion       │  │    Activo          │
 │    Cliente         │  │ FK IdProducto          │  │ FK IdProducto      │
 │    UniqueProducto  │  │ FK IdCategoria         │  └────────────────────┘
 │    Cantidad        │  └───────────┬────────────┘
 │    Precio          │              │ N
 │    Descuento       │              │
 │    Total           │              │ 1
 │ FK IdProducto      │              ▼
 └────────────────────┘   ┌────────────────────────┐
                          │      Categorias        │
                          │ PK IdCategoria         │
                          │    Descripcion         │
                          │    Activo              │
                          │ FK IdCategoriaPadre ───┼──┐ auto-referencia
                          └────────────────────────┘  │ (padre/hijo)
                                       ▲               │
                                       └───────────────┘
```

`◄UQ►` = índice único · `PK` = clave primaria · `FK` = clave foránea.

### Relaciones (cardinalidad)

| Origen | | Destino | Tipo | Detalle |
|--------|--|---------|------|---------|
| `TipoProducto` | 1 — N | `ExpProducto` | uno a muchos | un tipo agrupa varios productos |
| `ExpProducto` | 1 — 1 | `ErpProducto` | uno a uno | **comparten PK/FK** (`IdProducto`); ErpProducto extiende al Express |
| `ErpProducto` | 1 — N | `CodigoBarra` | uno a muchos | un producto ERP tiene varios códigos de barra |
| `ExpProducto` | 1 — N | `ProductoCategoria` | uno a muchos | lado producto de la tabla de unión |
| `Categoria` | 1 — N | `ProductoCategoria` | uno a muchos | lado categoría de la tabla de unión |
| `ExpProducto` ↔ `Categoria` | N — N | (vía `ProductoCategoria`) | muchos a muchos | un producto en varias categorías y viceversa |
| `ExpProducto` | 1 — N | `VentaExpress` | uno a muchos | un producto aparece en varias ventas |
| `Categoria` | 1 — N | `Categoria` | auto-referencia | jerarquía padre/hijo (`IdCategoriaPadre` nullable) |

### Decisiones de mapeo relevantes
- **1‑a‑1 con PK compartida** (`ExpProducto`/`ErpProducto`): no hay columna FK extra; `ErpProducto.IdProducto` es PK **y** FK. Configurado con `HasForeignKey<ErpProducto>(x => x.IdProducto)` + `ValueGeneratedNever()`.
- **`UniqueCodigo`** tiene índice único en `ErpProductos` y `CodigosBarras`; el servicio genera el valor y reintenta ante colisión.
- **`ProductosCategorias`** es tabla de unión **con payload** (`FechaCreacion`), por eso es una entidad explícita y no un many‑to‑many implícito.
- **`VentaExpress.UniqueProducto`** guarda una *copia* del `UniqueCodigo` del ERP al momento de la venta (RN2) — dato histórico, independiente de cambios posteriores.
- **Borrados**: `Cascade` donde la dependencia es total (ErpProducto, CodigosBarras, ProductosCategorias); `Restrict` donde borrar rompería historial o catálogo (VentasExpress, TipoProducto, Categoria padre).

---

## 8. Resumen del flujo inverso (lectura)

Para mostrar la lista de productos (opción 2/3/4 del menú):

```
Menu.ListarProductosAsync()
  → ProductoService.ListarProductosAsync()
    → IUnitOfWork.Productos.GetAllAsync()
      → Repository<ExpProducto>.GetAllAsync()
        → _context.Set<ExpProducto>().ToListAsync()
          → EF: SELECT * FROM ExpProductos
            → SQL Server devuelve filas → entidades → se imprimen en consola
```

Mismo principio, sentido inverso: SQL → entidades Domain → servicio → consola.
