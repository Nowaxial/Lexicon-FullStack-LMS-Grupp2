# LMS – Lexicon FullStack LMS (Grupp 2)

> **Branch:** `lms-lexicon-deploy`  
> Ett Learning Management System byggt med ASP.NET Core Web API, Blazor (Server + WebAssembly) och Entity Framework Core, designat för deployment till **Azure App Service**.

---

## Innehållsförteckning

- [Projektöversikt](#projektöversikt)
- [Teknikstack](#teknikstack)
- [Projektstruktur](#projektstruktur)
- [Krav för lokal körning](#krav-för-lokal-körning)
- [Konfiguration](#konfiguration)
- [Databasmigrationer](#databasmigrationer)
- [Köra projektet lokalt](#köra-projektet-lokalt)
- [Deploy till Azure](#deploy-till-azure)
- [API-dokumentation (Swagger)](#api-dokumentation-swagger)
- [Autentisering](#autentisering)

---

## Projektöversikt

LMS är ett webbaserat lärandehanteringssystem som låter lärare skapa kurser och moduler, ladda upp material och hantera studenter. Systemet är uppdelat i en REST API-backend och en Blazor-frontend med stöd för fillagring via **Azure Blob Storage**.

---

## Teknikstack

| Lager | Teknologi |
|---|---|
| Backend API | ASP.NET Core 8, C# |
| Frontend | Blazor Server + Blazor WebAssembly (Auto render mode) |
| Databas | SQL Server / Azure SQL (Entity Framework Core) |
| Autentisering | ASP.NET Core Identity + JWT Bearer |
| Fillagring | Azure Blob Storage (eller lokal `wwwroot/uploads`) |
| ORM / Migrationer | Entity Framework Core, Code First |
| API-dokumentation | Swagger / OpenAPI |
| Arkitektur | Clean Architecture (Onion) |

---

## Projektstruktur

```
LMS.sln
├── LMS.API/                  → ASP.NET Core Web API (entry point, Program.cs, controllers)
├── LMS.Blazor/               → Blazor Server host-applikation
├── LMS.Blazor.Client/        → Blazor WebAssembly-klient
├── LMS.Presentation/         → Presentationslogik och ViewModels
├── LMS.Services/             → Affärslogik (AuthService, CourseService, UserService m.fl.)
├── LMS.Infractructure/       → EF Core DbContext, Repositories, Migrations, Azure Blob Storage
├── LMS.Shared/               → Delade hjälpklasser och utilities
├── Domain.Models/            → Domänentiteter (Course, Module, User m.fl.)
├── Domain.Contracts/         → Interfaces för domänlagret
└── Service.Contracts/        → Interfaces för tjänstelagret
```

---

## Krav för lokal körning

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server (LocalDB räcker för lokal utveckling) eller Azure SQL
- Visual Studio 2022+ eller VS Code med C# Dev Kit
- (Valfritt) Azure-prenumeration för Blob Storage och App Service

---

## Konfiguration

Alla konfigurationer sker i `LMS.API/appsettings.json`. För lokal körning, använd `appsettings.Development.json` eller **User Secrets** för att undvika att checka in känsliga värden.

```json
{
  "ConnectionStrings": {
    "ApplicationDbContext": "Server=(localdb)\\mssqllocaldb;Database=LmsDB;Trusted_Connection=True;"
  },
  "JwtSettings": {
    "Issuer": "LmsAPI",
    "Audience": "https://localhost:7213",
    "Expires": 5
  },
  "AzureBlob": {
    "ConnectionString": "<din-azure-blob-connection-string>",
    "ContainerName": "<ditt-container-namn>"
  },
  "FileStorage": {
    "RootPath": "wwwroot/uploads",
    "PublicBasePath": "uploads"
  }
}
```

> ⚠️ **Lägg aldrig in riktiga connection strings eller hemliga nycklar i versionshistoriken.**  
> Använd **Azure App Service → Configuration → Application Settings** i produktion, eller **User Secrets** lokalt:
> ```bash
> dotnet user-secrets set "JwtSettings:SecretKey" "ditt-hemliga-värde" --project LMS.API
> ```

---

## Databasmigrationer

Migrationsfiler finns under `LMS.Infractructure/Migrations/`.

### Skapa ny migration
```bash
dotnet ef migrations add <MigrationsNamn> --project LMS.Infractructure --startup-project LMS.API
```

### Applicera migrationer lokalt
```bash
dotnet ef database update --project LMS.Infractructure --startup-project LMS.API
```

### Applicera migrationer i produktion (Azure)
I `Program.cs` finns en utkommenterad sektion för automatisk migration vid uppstart. Avkommentera vid behov:

```csharp
if (app.Environment.IsProduction())
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await context.Database.MigrateAsync();
}
```

---

## Köra projektet lokalt

1. Klona repot och byt till deploy-branchen:
   ```bash
   git clone https://github.com/Nowaxial/Lexicon-FullStack-LMS-Grupp2.git
   cd Lexicon-FullStack-LMS-Grupp2
   git checkout lms-lexicon-deploy
   ```

2. Återställ NuGet-paket:
   ```bash
   dotnet restore
   ```

3. Applicera databasmigrationer:
   ```bash
   dotnet ef database update --project LMS.Infractructure --startup-project LMS.API
   ```

4. Starta API:t:
   ```bash
   dotnet run --project LMS.API
   ```

5. Starta Blazor-frontend (separat terminal):
   ```bash
   dotnet run --project LMS.Blazor
   ```

API:t är som standard tillgängligt på `https://localhost:7213` och Swagger UI på `https://localhost:7213/swagger`.

---

## Deploy till Azure

Denna branch (`lms-lexicon-deploy`) är konfigurerad för deployment till **Azure App Service**.

### Förutsättningar
- En Azure-prenumeration
- Två App Services: en för `LMS.API` och en för `LMS.Blazor`
- En Azure SQL-databas
- (Valfritt) Azure Blob Storage-konto för filuppladdningar

### Steg-för-steg

1. **Skapa Azure SQL-databas** och kopiera connection string.

2. **Konfigurera Application Settings** på App Service för `LMS.API`:

   | Nyckel | Värde |
   |---|---|
   | `ConnectionStrings__ApplicationDbContext` | Azure SQL connection string |
   | `JwtSettings__SecretKey` | Din hemliga JWT-nyckel |
   | `JwtSettings__Issuer` | `LmsAPI` |
   | `JwtSettings__Audience` | URL till din Blazor App Service |
   | `AzureBlob__ConnectionString` | Blob Storage connection string |
   | `AzureBlob__ContainerName` | Namn på din Blob-container |

3. **Publicera LMS.API** till Azure App Service:
   ```bash
   dotnet publish LMS.API -c Release -o ./publish/api
   ```

4. **Publicera LMS.Blazor** till separat App Service:
   ```bash
   dotnet publish LMS.Blazor -c Release -o ./publish/blazor
   ```

5. **Kör migrationer** antingen via `dotnet ef database update` mot Azure SQL-strängen, eller aktivera automatisk migration i `Program.cs`.

### CORS
CORS är konfigurerat med policyn `"AllowAll"` i nuvarande läge. Uppdatera detta i `LMS.API/Extensions/` inför produktionsdeploy för att bara tillåta din Blazor-applikations URL.

---

## API-dokumentation (Swagger)

Swagger UI är aktiverat i alla miljöer. Nås på:

```
https://<din-api-url>/swagger
```

> 💡 Överväg att begränsa Swagger till Development-miljön i produktion genom att flytta det under `if (app.Environment.IsDevelopment())`.

---

## Autentisering

API:t använder **ASP.NET Core Identity** kombinerat med **JWT Bearer-tokens**.

- Registrering och inloggning hanteras av `AuthService` i `LMS.API/Services/`
- JWT-tokens genereras med inställningar från `JwtSettings` i `appsettings.json`
- Token-livslängd styrs av `Expires`-värdet (i minuter, standard: 5)
- Skyddade endpoints kräver `Authorization: Bearer <token>`-header

---

*README genererad baserat på källkod i branchen `lms-lexicon-deploy` – [Nowaxial/Lexicon-FullStack-LMS-Grupp2](https://github.com/Nowaxial/Lexicon-FullStack-LMS-Grupp2/tree/lms-lexicon-deploy)*
