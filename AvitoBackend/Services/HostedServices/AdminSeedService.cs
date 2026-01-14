using AvitoBackend.Data;
using AvitoBackend.Models;
using AvitoBackend.Models.Core;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AvitoLite.HostedServices;

public class AdminSeedService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AdminSeedService> _logger;

    public AdminSeedService(IServiceProvider serviceProvider, ILogger<AdminSeedService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Запуск инициализации: админ, категории, тестовые пользователи и объявления...");

        using var scope = _serviceProvider.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await context.Database.MigrateAsync(cancellationToken);

        await SeedAdminAsync(userManager, roleManager, cancellationToken);

        await SeedCategoriesAsync(context, cancellationToken);

        await SeedTestUsersAndAdsAsync(userManager, context, cancellationToken);
    }

    private async Task SeedAdminAsync(
        UserManager<AppUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        CancellationToken ct)
    {
        if (!await roleManager.RoleExistsAsync("Admin"))
        {
            await roleManager.CreateAsync(new IdentityRole<Guid>("Admin"));
            _logger.LogInformation("Создана роль Admin");
        }

        var adminEmail = "admin@example.com";
        var admin = await userManager.FindByEmailAsync(adminEmail);
        if (admin == null)
        {
            admin = new AppUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
            };

            var result = await userManager.CreateAsync(admin, "Admin123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(admin, "Admin");
                _logger.LogInformation("Создан администратор: {Email}", adminEmail);
            }
            else
            {
                _logger.LogError("Ошибка создания администратора: {Errors}",
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
        else
        {
            _logger.LogInformation("Администратор уже существует");
        }
    }

    private async Task SeedCategoriesAsync(AppDbContext context, CancellationToken ct)
    {
        if (await context.Categories.AnyAsync(ct))
        {
            _logger.LogInformation("Категории уже существуют, пропускаем инициализацию");
            return;
        }

        _logger.LogInformation("Создание популярных категорий...");

        var rootCategories = new List<Category>
        {
            new() { Id = Guid.NewGuid(), Name = "Недвижимость", Description = "Квартиры, дома, арендa и продажа" },
            new() { Id = Guid.NewGuid(), Name = "Транспорт", Description = "Автомобили, мотоциклы, запчасти" },
            new() { Id = Guid.NewGuid(), Name = "Электроника", Description = "Телефоны, ПК, техника" },
            new() { Id = Guid.NewGuid(), Name = "Работа", Description = "Вакансии, поиск сотрудников" },
            new() { Id = Guid.NewGuid(), Name = "Одежда и стиль", Description = "Одежда, обувь, аксессуары" },
            new() { Id = Guid.NewGuid(), Name = "Дом и сад", Description = "Мебель, инструменты, растения" },
            new() { Id = Guid.NewGuid(), Name = "Детские товары", Description = "Игрушки, коляски, одежда" },
            new() { Id = Guid.NewGuid(), Name = "Хобби и отдых", Description = "Книги, спорт, музыка, билеты" },
            new() { Id = Guid.NewGuid(), Name = "Животные", Description = "Питомцы, корм, аксессуары" },
            new() { Id = Guid.NewGuid(), Name = "Услуги", Description = "Ремонт, красота, IT, обучение" }
        };

        var electronics = rootCategories.First(c => c.Name == "Электроника");
        var transport = rootCategories.First(c => c.Name == "Транспорт");

        var subCategories = new List<Category>
        {
            new() { Id = Guid.NewGuid(), Name = "Смартфоны", ParentId = electronics.Id, Description = "iPhone, Samsung, Xiaomi" },
            new() { Id = Guid.NewGuid(), Name = "Ноутбуки", ParentId = electronics.Id, Description = "Игровые, офисные, Apple" },
            new() { Id = Guid.NewGuid(), Name = "Автомобили", ParentId = transport.Id, Description = "Легковые, новые и с пробегом" },
            new() { Id = Guid.NewGuid(), Name = "Мотоциклы", ParentId = transport.Id, Description = "Скутеры, мотоциклы, запчасти" }
        };

        await context.Categories.AddRangeAsync(rootCategories, ct);
        await context.SaveChangesAsync(ct);

        await context.Categories.AddRangeAsync(subCategories, ct);
        await context.SaveChangesAsync(ct);

        _logger.LogInformation("Создано {Count} категорий", rootCategories.Count + subCategories.Count);
    }

    private async Task SeedTestUsersAndAdsAsync(
        UserManager<AppUser> userManager,
        AppDbContext context,
        CancellationToken ct)
    {
        if (await context.Advertisements.AnyAsync(ct))
        {
            _logger.LogInformation("Объявления уже существуют, пропускаем инициализацию");
            return;
        }

        _logger.LogInformation("Создание тестовых пользователей и объявлений...");

        var testUsers = new[]
        {
            new { Email = "ivan@example.com",  Password = "User123!" },
            new { Email = "maria@example.com", Password = "User123!" },
            new { Email = "alex@example.com",  Password = "User123!" }
        };

        var users = new List<AppUser>();
        foreach (var u in testUsers)
        {
            var existing = await userManager.FindByEmailAsync(u.Email);
            if (existing == null)
            {
                var user = new AppUser
                {
                    Email = u.Email,
                    UserName = u.Email,
                    EmailConfirmed = true
                };
                var result = await userManager.CreateAsync(user, u.Password);
                if (result.Succeeded)
                {
                    users.Add(user);
                    _logger.LogInformation("Создан тестовый пользователь: {Email}", u.Email);
                }
                else
                {
                    _logger.LogError("Не удалось создать пользователя {Email}: {Errors}",
                        u.Email, string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
            else
            {
                users.Add(existing);
                _logger.LogInformation("Пользователь уже существует: {Email}", u.Email);
            }
        }


        var categories = await context.Categories
            .Where(c => c.ParentId == null) 
            .Take(3)
            .ToListAsync(ct);

        if (categories.Count < 3)
        {
            _logger.LogWarning("Недостаточно категорий для объявлений");
            return;
        }

        var ads = new List<Advertisement>
        {
            new()
            {
                Title = "Продаю iPhone 14",
                Description = "В отличном состоянии, 256 ГБ, синий",
                Price = 65000,
                CategoryId = categories[2].Id, 
                User = users[0],
                UserId = users[0].Id,
                ImageUrls = new List<string> { "https://example.com/iphone.jpg" }
            },
            new()
            {
                Title = "Сдам квартиру в центре",
                Description = "2-комнатная, евроремонт, метро рядом",
                Price = 45000,
                CategoryId = categories[0].Id, 
                                User = users[1],
                UserId = users[1].Id,
                ImageUrls = new List<string> { "https://example.com/apt.jpg" }
            },
            new()
            {
                Title = "Автомобиль Toyota Camry",
                Description = "2020 г., пробег 30 тыс. км, полный комплект",
                Price = 2200000,
                CategoryId = categories[1].Id, 
                                User = users[2],
                UserId = users[2].Id,
                ImageUrls = new List<string> { "https://example.com/car.jpg" }
            }
        };

        await context.Advertisements.AddRangeAsync(ads, ct);
        await context.SaveChangesAsync(ct);

        _logger.LogInformation("Создано {UserCount} пользователей и {AdCount} объявлений", users.Count, ads.Count);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}