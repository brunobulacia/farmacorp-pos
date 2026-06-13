# FarmacorpPOS — Prueba Técnica NEXOCORP

POS Express para Farmacorp. Arquitectura N-Capas con .NET, Entity Framework Core y SQL Server.

## Requisitos
- .NET 8 SDK (compila y corre también con SDK/runtime 10 vía `RollForward`)
- Docker Desktop
- Herramienta EF: `dotnet tool install --global dotnet-ef --version 8.0.10`

## Base de datos (SQL Server en Docker)
```bash
docker run -e ACCEPT_EULA=Y -e SA_PASSWORD=Farmacorp123! \
  -p 1433:1433 --name sqlserver --platform linux/amd64 \
  -d mcr.microsoft.com/mssql/server:2022-latest
```
Cadena de conexión en `FarmacorpPOS.Console/appsettings.json`.

## Ejecutar
```bash
# 1) Crear el esquema (desde Infrastructure, apuntando al startup project)
cd FarmacorpPOS.Infrastructure
dotnet ef database update --startup-project ../FarmacorpPOS.Console

# 2) Correr la app
cd ../FarmacorpPOS.Console
dotnet run
```

## Arquitectura
N-Capas:
- **Domain** — entidades (Express + ERP)
- **Infrastructure** — `FarmacorpDbContext` (Fluent API), `Repository<T>` genérico, `UnitOfWork`
- **Application** — reglas de negocio (Strategy) + `ProductoService` / `VentaService`
- **Console** — menú interactivo con IoC/DI

Patrones: Repository, UnitOfWork, **Strategy** (reglas `Base` / `GanaMax`, conmutables en runtime), IoC/DI.

## Reglas de negocio
| Regla | Base | GanaMax |
|-------|------|---------|
| Margen de precio | costo × 1.50 | costo × 1.80 |
| Descuento (única categoría) | 30% | 10% |
| Validación de stock | `stock ≥ cantidad` | `stock ≥ cantidad` y deja > 10 unidades |

Venta: RN2 (guarda `UniqueCodigo` del ERP), RN3 (descuento por nº de categorías),
RN4 (valida stock — lanza excepción si falla), RN5 (descuenta stock),
`Total = (Precio × Cantidad) × (1 − Descuento)`.

## Nota de entorno
Desarrollado en macOS Apple Silicon. Se usa SQL Server en Docker en lugar de
LocalDB (exclusivo de Windows), con comportamiento equivalente.
