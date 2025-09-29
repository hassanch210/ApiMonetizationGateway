using Microsoft.AspNetCore.Mvc;

namespace ApiMonetizationGateway.SampleApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly ILogger<ProductsController> _logger;
    private static readonly List<Product> Products = new()
    {
        new Product { Id = 1, Name = "Laptop", Price = 999.99m, Category = "Electronics" },
        new Product { Id = 2, Name = "Mouse", Price = 29.99m, Category = "Electronics" },
        new Product { Id = 3, Name = "Keyboard", Price = 79.99m, Category = "Electronics" },
        new Product { Id = 4, Name = "Monitor", Price = 299.99m, Category = "Electronics" },
        new Product { Id = 5, Name = "Desk Chair", Price = 199.99m, Category = "Furniture" }
    };

    public ProductsController(ILogger<ProductsController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    public ActionResult<IEnumerable<Product>> GetAllProducts()
    {
        _logger.LogInformation("All products requested at {Time}", DateTime.UtcNow);
        return Ok(Products);
    }

    [HttpGet("{id}")]
    public ActionResult<Product> GetProduct(int id)
    {
        _logger.LogInformation("Product {Id} requested at {Time}", id, DateTime.UtcNow);
        
        var product = Products.FirstOrDefault(p => p.Id == id);
        if (product == null)
        {
            return NotFound($"Product with ID {id} not found");
        }

        return Ok(product);
    }

    [HttpGet("category/{category}")]
    public ActionResult<IEnumerable<Product>> GetProductsByCategory(string category)
    {
        _logger.LogInformation("Products in category {Category} requested at {Time}", category, DateTime.UtcNow);
        
        var products = Products.Where(p => p.Category.Equals(category, StringComparison.OrdinalIgnoreCase)).ToList();
        return Ok(products);
    }

    [HttpGet("search")]
    public ActionResult<IEnumerable<Product>> SearchProducts([FromQuery] string? name = null, [FromQuery] decimal? minPrice = null, [FromQuery] decimal? maxPrice = null)
    {
        _logger.LogInformation("Product search requested with name: {Name}, minPrice: {MinPrice}, maxPrice: {MaxPrice} at {Time}", 
            name, minPrice, maxPrice, DateTime.UtcNow);

        var query = Products.AsQueryable();

        if (!string.IsNullOrEmpty(name))
        {
            query = query.Where(p => p.Name.Contains(name, StringComparison.OrdinalIgnoreCase));
        }

        if (minPrice.HasValue)
        {
            query = query.Where(p => p.Price >= minPrice.Value);
        }

        if (maxPrice.HasValue)
        {
            query = query.Where(p => p.Price <= maxPrice.Value);
        }

        return Ok(query.ToList());
    }

    [HttpPost]
    public ActionResult<Product> CreateProduct([FromBody] CreateProductRequest request)
    {
        _logger.LogInformation("New product creation requested: {Name} at {Time}", request.Name, DateTime.UtcNow);

        var newProduct = new Product
        {
            Id = Products.Max(p => p.Id) + 1,
            Name = request.Name,
            Price = request.Price,
            Category = request.Category
        };

        Products.Add(newProduct);

        return CreatedAtAction(nameof(GetProduct), new { id = newProduct.Id }, newProduct);
    }

    [HttpPut("{id}")]
    public ActionResult<Product> UpdateProduct(int id, [FromBody] CreateProductRequest request)
    {
        _logger.LogInformation("Product {Id} update requested at {Time}", id, DateTime.UtcNow);

        var product = Products.FirstOrDefault(p => p.Id == id);
        if (product == null)
        {
            return NotFound($"Product with ID {id} not found");
        }

        product.Name = request.Name;
        product.Price = request.Price;
        product.Category = request.Category;

        return Ok(product);
    }

    [HttpDelete("{id}")]
    public ActionResult DeleteProduct(int id)
    {
        _logger.LogInformation("Product {Id} deletion requested at {Time}", id, DateTime.UtcNow);

        var product = Products.FirstOrDefault(p => p.Id == id);
        if (product == null)
        {
            return NotFound($"Product with ID {id} not found");
        }

        Products.Remove(product);
        return NoContent();
    }
}

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Category { get; set; } = string.Empty;
}

public class CreateProductRequest
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Category { get; set; } = string.Empty;
}