using FarmacorpPOS.Application.Reglas;
using FarmacorpPOS.Application.Services;
using FarmacorpPOS.Console;
using FarmacorpPOS.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false)
    .Build();

var connectionString = config.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Falta la cadena de conexión 'DefaultConnection' en appsettings.json.");

var services = new ServiceCollection();

services.AddDbContext<FarmacorpDbContext>(options => options.UseSqlServer(connectionString));

services.AddScoped<IUnitOfWork, UnitOfWork>();
services.AddSingleton<ReglasNegocioProvider>();
services.AddScoped<ProductoService>();
services.AddScoped<VentaService>();

var provider = services.BuildServiceProvider();

await new Menu(provider).EjecutarAsync();
