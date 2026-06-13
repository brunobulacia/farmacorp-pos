using FarmacorpPOS.Application.Reglas;
using FarmacorpPOS.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FarmacorpPOS.Console;

public class Menu
{
    private readonly IServiceProvider _root;
    private readonly ReglasNegocioProvider _reglas;

    public Menu(IServiceProvider root)
    {
        _root = root;
        _reglas = root.GetRequiredService<ReglasNegocioProvider>();
    }

    public async Task EjecutarAsync()
    {
        var salir = false;
        while (!salir)
        {
            MostrarMenu();
            var opcion = (System.Console.ReadLine() ?? "").Trim();
            System.Console.WriteLine();
            try
            {
                switch (opcion)
                {
                    case "1": await RegistrarProductoAsync(); break;
                    case "2": await AsignarCodigoBarraAsync(); break;
                    case "3": await AsignarCategoriaAsync(); break;
                    case "4": await RegistrarVentaAsync(); break;
                    case "5": CambiarEstrategia(); break;
                    case "0": salir = true; break;
                    default: System.Console.WriteLine("Opción inválida."); break;
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Error: {ex.Message}");
            }

            if (!salir)
            {
                System.Console.WriteLine("\nPresione ENTER para continuar...");
                System.Console.ReadLine();
            }
        }
        System.Console.WriteLine("Hasta luego.");
    }

    private void MostrarMenu()
    {
        try { System.Console.Clear(); } catch { }
        System.Console.WriteLine("=== FARMACORP POS EXPRESS ===");
        System.Console.WriteLine($"Estrategia activa: {_reglas.Actual.Nombre}");
        System.Console.WriteLine();
        System.Console.WriteLine("1. Registrar nuevo producto ERP");
        System.Console.WriteLine("2. Asignar código de barras");
        System.Console.WriteLine("3. Asignar categoría a producto");
        System.Console.WriteLine("4. Registrar venta");
        System.Console.WriteLine("5. Cambiar estrategia (Base / GanaMax)");
        System.Console.WriteLine("0. Salir");
        System.Console.Write("\nOpción: ");
    }

    private async Task RegistrarProductoAsync()
    {
        var nombre = Pedir("Nombre del producto");
        var costo = PedirDecimal("Costo");
        var stock = PedirInt("Stock inicial");
        var fechaVenc = PedirFecha("Fecha de vencimiento (yyyy-MM-dd)");
        var obs = Pedir("Observaciones", permitirVacio: true);

        using var scope = _root.CreateScope();
        var svc = scope.ServiceProvider.GetRequiredService<ProductoService>();
        var p = await svc.RegistrarProductoErpAsync(nombre, fechaVenc, obs, costo, stock);

        System.Console.WriteLine($"\nProducto registrado. Id={p.IdProducto} | Precio={p.Precio:0.00} " +
                                 $"| UniqueCodigo={p.ErpProducto.UniqueCodigo} (estrategia {_reglas.Actual.Nombre})");
    }

    private async Task AsignarCodigoBarraAsync()
    {
        await ListarProductosAsync();
        var id = PedirInt("Id del producto");

        using var scope = _root.CreateScope();
        var svc = scope.ServiceProvider.GetRequiredService<ProductoService>();
        var cb = await svc.AsignarCodigoBarraAsync(id);
        System.Console.WriteLine($"\nCódigo de barras asignado: {cb.UniqueCodigo}");
    }

    private async Task AsignarCategoriaAsync()
    {
        await ListarProductosAsync();
        var idProd = PedirInt("Id del producto");

        using var scope = _root.CreateScope();
        var svc = scope.ServiceProvider.GetRequiredService<ProductoService>();

        var categorias = (await svc.ListarCategoriasAsync()).ToList();
        if (categorias.Count == 0)
        {
            System.Console.WriteLine("No hay categorías. Cree una nueva.");
            var desc = Pedir("Descripción de la nueva categoría");
            var nueva = await svc.CrearCategoriaAsync(desc);
            categorias.Add(nueva);
        }
        else
        {
            System.Console.WriteLine("\nCategorías:");
            foreach (var c in categorias)
                System.Console.WriteLine($"  {c.IdCategoria}. {c.Descripcion}");
            System.Console.WriteLine("  0. + Crear nueva categoría");
        }

        var idCat = PedirInt("Id de la categoría (0 para crear)");
        if (idCat == 0)
        {
            var desc = Pedir("Descripción de la nueva categoría");
            var nueva = await svc.CrearCategoriaAsync(desc);
            idCat = nueva.IdCategoria;
        }

        var det = await svc.AsignarCategoriaAsync(idProd, idCat);
        System.Console.WriteLine($"\nCategoría asignada (detalle Id={det.IdDetalle}).");
    }

    private async Task RegistrarVentaAsync()
    {
        await ListarProductosAsync();
        var idProd = PedirInt("Id del producto");
        var cliente = Pedir("Cliente");
        var cantidad = PedirInt("Cantidad");

        using var scope = _root.CreateScope();
        var svc = scope.ServiceProvider.GetRequiredService<VentaService>();
        var v = await svc.RegistrarVentaAsync(idProd, cliente, cantidad);

        System.Console.WriteLine($"\nVenta registrada. Id={v.Id}");
        System.Console.WriteLine($"   Precio unit.: {v.Precio:0.00} | Descuento: {v.Descuento:P0} | Total: {v.Total:0.00}");
        System.Console.WriteLine($"   UniqueProducto: {v.UniqueProducto}");
    }

    private void CambiarEstrategia()
    {
        System.Console.WriteLine("1. BASE");
        System.Console.WriteLine("2. GANAMAX");
        var op = Pedir("Seleccione estrategia");
        _reglas.Cambiar(op == "2" ? TipoEstrategia.GanaMax : TipoEstrategia.Base);
        System.Console.WriteLine($"\nEstrategia activa: {_reglas.Actual.Nombre}");
    }

    private async Task ListarProductosAsync()
    {
        using var scope = _root.CreateScope();
        var svc = scope.ServiceProvider.GetRequiredService<ProductoService>();
        var productos = (await svc.ListarProductosAsync()).ToList();
        if (productos.Count == 0)
        {
            System.Console.WriteLine("(No hay productos registrados aún.)");
            return;
        }
        System.Console.WriteLine("Productos:");
        foreach (var p in productos)
            System.Console.WriteLine($"  {p.IdProducto}. {p.Nombre} | Precio={p.Precio:0.00}");
        System.Console.WriteLine();
    }

    private static string Pedir(string etiqueta, bool permitirVacio = false)
    {
        while (true)
        {
            System.Console.Write($"{etiqueta}: ");
            var val = (System.Console.ReadLine() ?? "").Trim();
            if (permitirVacio || val.Length > 0) return val;
            System.Console.WriteLine("  Valor requerido.");
        }
    }

    private static int PedirInt(string etiqueta)
    {
        while (true)
        {
            if (int.TryParse(Pedir(etiqueta), out var n)) return n;
            System.Console.WriteLine("  Ingrese un número entero válido.");
        }
    }

    private static decimal PedirDecimal(string etiqueta)
    {
        while (true)
        {
            if (decimal.TryParse(Pedir(etiqueta), out var d)) return d;
            System.Console.WriteLine("  Ingrese un número válido.");
        }
    }

    private static DateTime PedirFecha(string etiqueta)
    {
        while (true)
        {
            if (DateTime.TryParse(Pedir(etiqueta), out var f)) return f;
            System.Console.WriteLine("  Ingrese una fecha válida.");
        }
    }
}
