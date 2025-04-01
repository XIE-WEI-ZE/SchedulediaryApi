using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SchedulediaryApi.Services;
using Serilog;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// 設定 Serilog 日誌記錄
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// JWT 設定
var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSettings["Key"];

// 驗證密鑰長度（至少 256 位元，32 字元）
if (string.IsNullOrEmpty(secretKey) || secretKey.Length < 32)
{
    throw new InvalidOperationException("JWT 密鑰長度不足，請在 appsettings.json 中設置至少 32 字元的 'Jwt:Key'。");
}

// 加入 JWT 驗證
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
    };

    // 加入認證失敗時的日誌記錄
    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            Log.Error("JWT 認證失敗: {Exception}", context.Exception.Message);
            return Task.CompletedTask;
        },
        OnChallenge = context =>
        {
            Log.Warning("JWT 挑戰觸發: {Error}, {ErrorDescription}", context.Error, context.ErrorDescription);
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

// 修改後的 CORS 設定（指定策略名稱）
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp", policy =>
    {
        policy.WithOrigins("http://localhost:4200") // 前端 Angular 應用程式的來源
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // 支援 cookie（如果需要）
    });
});

// 加入 Swagger（含 JWT Bearer 支援）
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "請輸入 'Bearer {token}'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new List<string>()
        }
    });
});

// 註冊服務
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<ScheduleRepository>();

var app = builder.Build();

// 開發環境開啟 Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 啟用中介軟體
app.UseCors("AllowAngularApp"); // 指定 CORS 策略名稱
app.UseHttpsRedirection();
app.UseAuthentication(); // JWT 驗證
app.UseAuthorization();  // 權限檢查

app.MapControllers();

// 加入全局異常處理中介軟體（可選）
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exceptionHandlerPathFeature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerPathFeature>();
        if (exceptionHandlerPathFeature?.Error != null)
        {
            Log.Error(exceptionHandlerPathFeature.Error, "未處理的異常: {Message}", exceptionHandlerPathFeature.Error.Message);
            context.Response.StatusCode = 500;
            await context.Response.WriteAsync("伺服器發生錯誤，請查看日誌以獲取更多資訊。");
        }
    });
});

app.Run();
//2025-03-30