using KeremProject1backend.Models.DBs;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using System.Text.Json.Serialization;
using KeremProject1backend.Services;
using System.Net.Http;
using Microsoft.Extensions.Options;
using KeremProject1backend.Filters;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using System.Net;
using Microsoft.AspNetCore.WebUtilities;

var builder = WebApplication.CreateBuilder(args);

// Ortam değişkenlerinin yapılandırmaya eklenmesi
builder.Configuration.AddEnvironmentVariables(prefix: "KEREMPROJECT_");

// --- ConfigDef Ayarlarını Yapılandırma ---
// appsettings.json dosyasındaki "ConfigDef" bölümünü okuyup ConfigDef sınıfına bağla
builder.Services.Configure<ConfigDef>(builder.Configuration.GetSection("ConfigDef"));
// ConfigDef'i DI container'a ekle (IOptions<ConfigDef> olarak kullanılabilir)
builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<ConfigDef>>().Value);
// -------------------------------------

// --- Servis Kayıtları ---
builder.Services.AddScoped<PayTrService>();
builder.Services.AddHttpClient<ShippingService>();
builder.Services.AddScoped<FileService>();
// Yeni: EmailService ekle
builder.Services.AddScoped<EmailService>();
// ----------------------

// Service registration
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
});

builder.Services.AddDbContext<GeneralContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("GeneralDb"))); // "GeneralConnection" ? "GeneralDb"

builder.Services.AddDbContext<UsersContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("UsersDb"))); // "UsersConnection" ? "UsersDb"

// YENİ: TestContext kaydı eklendi
builder.Services.AddDbContext<TestContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("TestDb"))); // Bağlantı dizesi adını kontrol edin

// CORS ayarları
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Swagger (DEMED)
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "KeremProject1Db", Version = "v1" });
    
    // IFormFile desteği için Swagger konfigürasyonu
    // c.SchemaFilter<FileUploadSchemaFilter>();
    
    c.AddSecurityDefinition("Token", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Please enter your API key in the 'Token' header.",
        Name = "Token",
        Type = SecuritySchemeType.ApiKey
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Token"
                }
            },
            new string[] { }
        }
    });
});

// --- Ek Servisler ---
builder.Services.AddHttpContextAccessor();
builder.Services.AddLogging();
// --------------------

builder.WebHost.UseUrls("http://*:5291", "https://*:7049");

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Your API V1");
        c.RoutePrefix = string.Empty;
    });
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");

// Authentication/Authorization middleware (mevcutsa)
// app.UseAuthentication(); // Eğer JWT veya Identity kullanıyorsan
app.UseAuthorization();

app.MapControllers();

// 🚀 Otomatik Database Migration
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        // GeneralContext migration
        var generalContext = services.GetRequiredService<GeneralContext>();
        await generalContext.Database.MigrateAsync();
        
        // UsersContext migration
        var usersContext = services.GetRequiredService<UsersContext>();
        await usersContext.Database.MigrateAsync();
        
        // TestContext migration
        var testContext = services.GetRequiredService<TestContext>();
        await testContext.Database.MigrateAsync();
        
        Console.WriteLine("✅ Database migration tamamlandı!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Migration hatası: {ex.Message}");
    }
}

app.Run();