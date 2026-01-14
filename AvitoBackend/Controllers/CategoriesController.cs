// Controllers/CategoriesController.cs
using AvitoBackend.Data;
using AvitoBackend.DTOs;
using AvitoBackend.Models;
using AvitoBackend.Models.Core;
using HotChocolate.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AvitoBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly AppDbContext _context;

    public CategoriesController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Получить все категории
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<CategoryDto>>> GetCategories()
    {
        var categories = await _context.Categories
            .OrderBy(c => c.Name)
            .Select(c => new CategoryDto 
            { 
                Id = c.Id, 
                Name = c.Name, 
                Description = c.Description,
                CreatedAt = c.CreatedAt
            })
            .ToListAsync();

        return Ok(categories);
    }

    /// <summary>
    /// Получить категорию по ID
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CategoryDto>> GetCategory(Guid id)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category == null)
            return NotFound($"Категория с ID {id} не найдена");

        return Ok(new CategoryDto 
        { 
            Id = category.Id, 
            Name = category.Name, 
            Description = category.Description,
            CreatedAt = category.CreatedAt
        });
    }

    /// <summary>
    /// Создать новую категорию
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<CategoryDto>> CreateCategory(CreateCategoryDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest("Название категории обязательно");

        var category = new Category 
        { 
            Name = dto.Name.Trim(),
            Description = dto.Description?.Trim()
        };

        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetCategory), 
            new { id = category.Id }, 
            new CategoryDto 
            { 
                Id = category.Id, 
                Name = category.Name, 
                Description = category.Description,
                CreatedAt = category.CreatedAt
            });
    }

    /// <summary>
    /// Обновить категорию
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CategoryDto>> UpdateCategory(Guid id, UpdateCategoryDto dto)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category == null)
            return NotFound($"Категория с ID {id} не найдена");

        if (!string.IsNullOrWhiteSpace(dto.Name))
            category.Name = dto.Name.Trim();
        
        if (dto.Description != null)
            category.Description = dto.Description.Trim();

        await _context.SaveChangesAsync();

        return Ok(new CategoryDto 
        { 
            Id = category.Id, 
            Name = category.Name, 
            Description = category.Description,
            CreatedAt = category.CreatedAt
        });
    }

    /// <summary>
    /// Удалить категорию
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteCategory(Guid id)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category == null)
            return NotFound($"Категория с ID {id} не найдена");

        // Проверка на наличие связанных объявлений
        var hasAds = await _context.Advertisements.AnyAsync(a => a.CategoryId == id);
        if (hasAds)
            return Conflict("Нельзя удалить категорию, пока в ней есть объявления");

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}