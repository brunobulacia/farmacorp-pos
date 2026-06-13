# FarmacorpPOS — Prueba Técnica NEXOCORP

POS Express para Farmacorp. Arquitectura N-Capas con .NET, Entity Framework Core y SQL Server.


## Arquitectura
N-Capas:
- **Domain** — entidades (Express + ERP)
- **Infrastructure** — `FarmacorpDbContext` (Fluent API), `Repository<T>` genérico, `UnitOfWork`
- **Application** — reglas de negocio (Strategy) + `ProductoService` / `VentaService`
- **Console** — menú interactivo con IoC/DI

Patrones: Repository, UnitOfWork, **Strategy** (reglas `Base` / `GanaMax`, conmutables en runtime), IoC/DI.
