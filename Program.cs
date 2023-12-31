using System.Reflection;
using Auth;
using HanumPay.Contexts;
using HanumPay.Core.Authentication;
using HanumPay.Core.Middleware;
using HanumPay.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Console;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseStaticWebAssets();

builder.Services.AddLogging(logging =>
    logging.AddSimpleConsole(options => {
        options.SingleLine = true;
        options.TimestampFormat = "HH:mm:ss ";
        options.ColorBehavior = LoggerColorBehavior.Enabled;
    })
);

builder.Services.AddCors(options => {
    options.AddPolicy(
        name: "AllowAll",
        policyBuilder => {
            policyBuilder.AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader();
        }
    );
    options.AddPolicy(
        name: "AllowCors",
        policyBuilder => {
            policyBuilder.WithOrigins(builder.Configuration.GetSection("AllowedCorsOrigins").Get<string[]>()!)
                .AllowAnyMethod()
                .AllowAnyHeader();
        }
    );
});

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options => {
    options.IncludeXmlComments(Path.Combine(
        AppContext.BaseDirectory,
        $"{Assembly.GetExecutingAssembly().GetName().Name}.xml"
    ));
});

// DB Context
var databaseConfig = builder.Configuration.GetSection("Database");
var connectionString = builder.Configuration.GetConnectionString("Database.SQL");
var dbOptionsBuilder = (DbContextOptionsBuilder options) => {
    options.UseMySql(
        connectionString,
        ServerVersion.AutoDetect(connectionString),
        options => options.EnableRetryOnFailure(
            maxRetryCount: databaseConfig.GetValue<int>("MaxRetryCount"),
            maxRetryDelay: TimeSpan.FromSeconds(databaseConfig.GetValue<int>("MaxRetryDelay")),
            errorNumbersToAdd: null
        )
    );
};

builder.Services.AddDbContextPool<HanumContext>(dbOptionsBuilder, databaseConfig.GetValue<int>("MaxPoolSize"));
builder.Services.AddDbContextFactory<HanumContext>(dbOptionsBuilder);
// Redis Cache
builder.Services.AddStackExchangeRedisCache(options => {
    options.Configuration = builder.Configuration.GetConnectionString("Cache.Redis");
});

// gRPC Client
builder.Services.AddGrpcClient<AuthService.AuthServiceClient>(options => {
    options.Address = new(builder.Configuration.GetConnectionString("AuthService.gRPC")!);
});

// Authentication Handler
builder.Services.AddAuthentication()
    .AddScheme<AuthenticationSchemeOptions, HanumAuthenticationHandler>(HanumAuthenticationHandler.SchemeName, null)
    .AddScheme<AuthenticationSchemeOptions, HanumBoothAuthenticationHandler>(HanumBoothAuthenticationHandler.SchemeName, null);

builder.Services.AddTransient<EoullimBoothService>();

var app = builder.Build();

if (app.Environment.IsDevelopment()) {
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseHsts();
    app.UseCors("AllowAll");
} else {
    app.UseExceptionHandler("/Error");
    app.UseCors("AllowCors");
}

app.UseMiddleware<RequestLoggingMiddleware>();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");


app.Run();
