# Learning Management System (LMS)

Ett modernt och fullständigt Learning Management System byggt med .NET 9, Blazor WebAssembly och ASP.NET Core. Systemet stödjer kurshantering, moduler, aktiviteter, dokumenthantering och krypterade realtidsnotifieringar.





## 📋 Innehåll

- [Översikt](#-översikt)
- [Screenshot](#screenshot)
- [Funktioner](#-funktioner)
- [Teknisk Stack](#-teknisk-stack)
- [Projektstruktur](#-projektstruktur)
- [Installation](#-installation)
- [API Dokumentation](#api-dokumentation)
- [Användarroller](#-användarroller)
- [Säkerhet](#-säkerhet)
- [Felsökning](#-felsökning)
- [Gruppmedlemmar](#-gruppmedlemmar)

## 🎯 Översikt

Detta LMS är utvecklat som ett gruppprojekt för att demonstrera fullstack .NET-utveckling med moderna designmönster och arkitektur. Systemet hanterar kurser, moduler, aktiviteter och dokumentinlämningar med rollbaserad åtkomstkontroll och krypterade notifieringar.

## Screenshots

### Desktop
![Desktop](https://i.ibb.co/NnxvyGsH/LMS-Lexicon-Desktop.jpg)

### Mobile
![Mobile](https://i.ibb.co/bMqcfNNV/LMS-Lexicon-Mobile.jpg)

### Swagger
![Swagger API](https://i.ibb.co/Y7qJL1Ys/Swagger-API.jpg)




## ✨ Funktioner

### För Lärare
- **Kurshantering**: Skapa, redigera och ta bort kurser med datum och beskrivningar
- **Modulhantering**: Organisera kurser i moduler med tidsperioder
- **Aktivitetshantering**: Skapa föreläsningar, övningar, projekt och kunskapskontroller
- **Dokumentbedömning**: Granska och betygsätta studentinlämningar (Godkänd/Underkänd/Granskning)
- **Användarhantering**: Tilldela lärare och studenter till kurser
- **Krypterade notifieringar**: Ta emot säkra meddelanden om studentaktiviteter

### För Studenter
- **Kursöversikt**: Visa tilldelade kurser och moduler med tidslinjer
- **Dokumentinlämning**: Ladda upp filer för aktiviteter med filvalidering
- **Statusöversikt**: Se bedömningar och feedback från lärare
- **Notifieringar**: Realtidsuppdateringar om kursändringar och bedömningar

### Gemensamma funktioner
- JWT-baserad autentisering med refresh tokens
- Responsiv Blazor-frontend med Server/WebAssembly hybrid rendering
- Filuppladdning med lokalt lagringssystem
- Avancerad sökfunktionalitet för kurser, moduler och användare
- Kontaktformulär med krypterad meddelandehantering
- Automatisk data seeding för utvecklingsmiljö

## 🛠 Teknisk Stack

### Backend
- **.NET 9** - Framework
- **ASP.NET Core Web API** - RESTful API
- **Entity Framework Core 9** - ORM och databashantering
- **SQL Server (LocalDB)** - Datalagring
- **AutoMapper** - DTO-mappning
- **JWT Bearer Authentication** - Token-baserad säkerhet
- **Bogus** - Realistisk testdata generering

### Frontend
- **Blazor WebAssembly** - SPA-framework
- **Blazor Server** - Hybrid rendering med SignalR
- **Bootstrap 5** - Responsiv UI-styling
- **Bootstrap Icons** - Ikonbibliotek
- **JavaScript Interop** - Custom UI-interaktioner

### Säkerhet
- **ASP.NET Core Identity** - Användarhantering och autentisering
- **AES Encryption** - Kryptering av känsliga notifieringar
- **Password Hashing** - PBKDF2 med 10,000 iterationer
- **CORS Policy** - Cross-Origin Resource Sharing konfiguration

### Arkitektur
- **Clean Architecture** med separerade lager:
  - **Domain** (Entities & Contracts) - Domänmodeller och interface
  - **Infrastructure** (Repositories & Data Access) - Dataåtkomst och persistens
  - **Application** (Services & Business Logic) - Affärslogik
  - **Presentation** (Controllers & API) - API-endpoints
  - **UI** (Blazor Components) - Användargränssnitt

### Designmönster
- **Repository Pattern** med Unit of Work
- **Service Layer Pattern** för affärslogik
- **Dependency Injection** i alla lager
- **Lazy Loading** av tjänster för prestanda
- **Factory Pattern** för användaranspråk

## 📁 Projektstruktur

```

LMS/
├── Domain.Models/              \# Entiteter och domänmodeller
│   ├── Entities/               \# Course, Module, Activity, Document, User
│   ├── Configurations/         \# JWT-inställningar
│   └── Exceptions/             \# Custom exceptions (NotFoundException, etc.)
│
├── Domain.Contracts/           \# Repository-interface
│   └── Repositories/           \# ICourseRepository, IModuleRepository, etc.
│
├── LMS.Infrastructure/         \# Dataåtkomst och repositories
│   ├── Data/                   \# DbContext, MapperProfile, Configurations
│   ├── Migrations/             \# EF Core migrationer (14 migrations)
│   ├── Repositories/           \# Concrete repository implementations
│   └── Storage/                \# LocalFileStorage implementation
│
├── LMS.Services/               \# Business logic services
│   ├── AuthService.cs          \# Autentisering och token-hantering
│   ├── CourseService.cs        \# Kurslogik
│   ├── ModuleService.cs        \# Modullogik
│   ├── ProjActivityService.cs  \# Aktivitetslogik
│   ├── ProjDocumentService.cs  \# Dokumenthantering
│   ├── UserService.cs          \# Användarhantering
│   ├── NotificationService.cs  \# Notifieringshantering
│   └── EncryptionService.cs    \# AES-kryptering
│
├── Service.Contracts/          \# Service-interface
│   └── Storage/                \# IFileStorage
│
├── LMS.Presentation/           \# API Controllers
│   ├── AuthController.cs       \# Login, register, token refresh
│   ├── CoursesController.cs    \# CRUD för kurser
│   ├── ModuleController.cs     \# CRUD för moduler
│   ├── ProjActivitiesController.cs
│   ├── ProjDocumentsController.cs
│   ├── UsersController.cs      \# Användarhantering
│   ├── NotificationsController.cs
│   ├── SearchController.cs     \# Global sökning
│   └── ContactController.cs    \# Kontaktformulär
│
├── LMS.Shared/                 \# DTOs för client-server kommunikation
│   └── DTOs/                   \# Auth, Course, Module, Activity, Document DTOs
│
├── LMS.API/                    \# API-projekt (huvudapplikation)
│   ├── Program.cs              \# DI-konfiguration och middleware
│   ├── Extensions/             \# Service extensions
│   ├── Services/               \# DataSeedHostingService
│   └── wwwroot/uploads/        \# Filuppladdningar
│
├── LMS.Blazor/                 \# Blazor Server-projekt
│   ├── Components/
│   │   ├── Account/            \# Login, Register, Manage
│   │   ├── Layout/             \# MainLayout, NavMenu, Footer
│   │   ├── Pages/              \# Home, Contact, Error
│   │   └── NotificationBell.razor
│   ├── Controller/             \# ProxyController
│   ├── Data/                   \# ApplicationDbContext
│   └── Services/               \# TokenStorage, AuthStateProvider
│
└── LMS.Blazor.Client/          \# Blazor WebAssembly-projekt
├── Components/             \# Återanvändbara komponenter
│   ├── ManageCourses.razor
│   ├── ManageModules.razor
│   ├── ManageUsers.razor
│   ├── ModuleStrip.razor
│   ├── NavStrip.razor
│   ├── SearchBar.razor
│   └── CourseComponents/
│       ├── StudentDocuments.razor
│       └── UploadFileModal.razor
├── Pages/                  \# Huvudsidor
│   ├── Courses.razor       \# Kursöversikt
│   ├── DetailsCoursePage.razor
│   ├── StudentPage.razor   \# Studentvy
│   ├── TeacherPage.razor   \# Lärarvy
│   └── About.razor
└── Services/               \# Client-side API services
├── ClientApiService.cs
├── DocumentsClient.cs
└── AuthReadyService.cs

```

## 🚀 Installation

### Förutsättningar
- **.NET 9 SDK** ([Ladda ner här](https://dotnet.microsoft.com/download/dotnet/9.0))
- **Visual Studio 2022/2026** eller **VS Code** med C# extension
- **SQL Server LocalDB** (ingår i Visual Studio)
- **Git** för versionskontroll

### Steg 1: Klona projektet
```bash
git clone https://github.com/nowaxial/lexicon-fullstack-lms-grupp2.git
cd lexicon-fullstack-lms-grupp2
```


### Steg 2: Konfigurera User Secrets

#### API-projektet (LMS.API)

```bash
cd LMS.API
dotnet user-secrets set "password" "YourDevPassword123!"
dotnet user-secrets set "JwtSettings:secretkey" "YourSecretKeyMustBeAtLeast32CharactersLong!!!!!!!!!!!!!!"
dotnet user-secrets set "EncryptionKey" "YourEncryptionKey12345678901234"
```

**Eller via Visual Studio:**

1. Högerklicka på `LMS.API` → **Manage User Secrets**
2. Lägg till:
```json
{
  "password": "YourDevPassword123!",
  "JwtSettings": {
    "secretkey": "YourSecretKeyMustBeAtLeast32CharactersLong!!!!!!!!!!!!!!"
  },
  "EncryptionKey": "YourEncryptionKey12345678901234"
}
```


#### Blazor-projektet (LMS.Blazor)

```bash
cd ../LMS.Blazor
dotnet user-secrets set "EncryptionKey" "YourEncryptionKey12345678901234"
```

**Eller via Visual Studio:**

1. Högerklicka på `LMS.Blazor` → **Manage User Secrets**
2. Lägg till:
```json
{
  "EncryptionKey": "YourEncryptionKey12345678901234"
}
```

> ⚠️ **KRITISKT**: `EncryptionKey` måste vara **identisk i båda projekten** för att kryptering/dekryptering av notifieringar ska fungera!

### Steg 3: Uppdatera databas

```bash
cd ../LMS.API
dotnet ef database update
```

Om du behöver installera EF Core tools:

```bash
dotnet tool install --global dotnet-ef
```


### Steg 4: Kör projekten

**Metod 1: Visual Studio (Rekommenderat)**

1. Öppna `LMS.sln` i Visual Studio
2. Högerklicka på Solution → **Set Startup Projects**
3. Välj **Multiple startup projects**
4. Sätt både `LMS.API` och `LMS.Blazor` till **Start**
5. Tryck **F5** eller klicka på **Start**

**Metod 2: Kommandoraden**

Terminal 1 (API):

```bash
cd LMS.API
dotnet run
```

Terminal 2 (Blazor):

```bash
cd LMS.Blazor
dotnet run
```


### Steg 5: Öppna applikationen

- **API Swagger**: https://localhost:7213/swagger
- **Blazor App**: https://localhost:7224 (port kan variera)


### Standard inloggningar

Efter första körningen skapas automatiskt testanvändare:


| Roll | Email | Lösenord |
| :-- | :-- | :-- |
| Lärare | `teacher@test.com` | Ditt lösenord från user secrets |
| Student | `student@test.com` | Ditt lösenord från user secrets |

Dessutom skapas:

- **6 lärare** (random svenska namn)
- **30 studenter** (random svenska namn)
- **6 kurser** med realistiska moduler och aktiviteter


## 📡 API Dokumentation

API:et är självdokumenterande med **Swagger UI**. Efter start, besök:

```
https://localhost:7213/swagger
```


### Huvudendpoints

#### Autentisering

```
POST   /api/auth/login           - Logga in (returnerar JWT + refresh token)
POST   /api/auth/register        - Registrera ny användare
POST   /api/auth/refresh         - Förnya access token
```


#### Kurser

```
GET    /api/courses              - Hämta alla kurser (paginerad)
GET    /api/courses/{id}         - Hämta specifik kurs med moduler
POST   /api/courses              - Skapa ny kurs [Teacher]
PUT    /api/courses/{id}         - Uppdatera kurs [Teacher]
DELETE /api/courses/{id}         - Ta bort kurs [Teacher]
POST   /api/courses/{id}/assign  - Tilldela användare till kurs [Teacher]
```


#### Moduler

```
GET    /api/modules              - Hämta alla moduler
GET    /api/modules/{id}         - Hämta specifik modul med aktiviteter
POST   /api/modules              - Skapa ny modul [Teacher]
PUT    /api/modules/{id}         - Uppdatera modul [Teacher]
DELETE /api/modules/{id}         - Ta bort modul [Teacher]
```


#### Aktiviteter

```
GET    /api/projactivities                    - Hämta alla aktiviteter
GET    /api/projactivities/{id}               - Hämta specifik aktivitet
GET    /api/projactivities/module/{moduleId}  - Hämta aktiviteter per modul
POST   /api/projactivities                    - Skapa aktivitet [Teacher]
PUT    /api/projactivities/{id}               - Uppdatera aktivitet [Teacher]
DELETE /api/projactivities/{id}               - Ta bort aktivitet [Teacher]
```


#### Dokument

```
GET    /api/projdocuments                - Hämta dokument (filtrerat per roll)
GET    /api/projdocuments/{id}           - Hämta specifikt dokument
POST   /api/projdocuments/upload         - Ladda upp dokument [Student/Teacher]
PUT    /api/projdocuments/{id}/status    - Sätt bedömningsstatus [Teacher]
GET    /api/projdocuments/download/{id}  - Ladda ner dokument
DELETE /api/projdocuments/{id}           - Ta bort dokument [Teacher]
```


#### Användare

```
GET    /api/users                - Hämta alla användare [Teacher]
GET    /api/users/{id}           - Hämta specifik användare
POST   /api/users/{id}/roles     - Sätt användarroller [Teacher]
```


#### Notifieringar

```
GET    /api/notifications        - Hämta användarens notifieringar
PUT    /api/notifications/{id}/read    - Markera som läst
DELETE /api/notifications/{id}   - Ta bort notifiering
```


#### Sökning

```
GET    /api/search?query={text}  - Sök över kurser, moduler och användare
```


## 👥 Användarroller

### Teacher (Lärare)

- Full CRUD på kurser, moduler och aktiviteter
- Betygsätta studentinlämningar med status (Godkänd/Underkänd/Granskning)
- Tilldela användare till kurser
- Visa alla dokument för sina kurser
- Ta emot krypterade notifieringar om studentaktiviteter
- Hantera kontaktmeddelanden från studenter


### Student

- Visa tilldelade kurser och moduler
- Ladda upp dokument för aktiviteter (PDF, Word, PowerPoint, etc.)
- Visa egna bedömningar och status
- Läsa kursbeskrivningar och aktivitetsinformation
- Skicka kontaktmeddelanden till lärare


## 🔐 Säkerhet

### Autentisering

- **JWT-tokens** med expiration (5 minuter)
- **Refresh tokens** med 30 dagars livslängd
- **Token rotation** vid refresh
- **Secure cookie storage** för tokens i Blazor


### Auktorisering

- **Rollbaserad åtkomstkontroll** (`[Authorize(Roles = "Teacher")]`)
- **Claims-baserad** användarprofil (FirstName, LastName i navbar)
- **Resource-baserad** åtkomst (studenter ser bara sina egna dokument)


### Dataskydd

- **Password hashing** med ASP.NET Identity (PBKDF2, 10,000 iterationer)
- **AES-256 kryptering** för känsliga notifieringar
- **Krypterad meddelandehantering** för kontaktformulär
- **Fil-validering** vid uppladdning (typ, storlek)
- **CORS-konfiguration** för Blazor-klient


### Database Security

- **Parameteriserade queries** via EF Core
- **SQL injection-skydd** automatiskt via ORM
- **Connection strings** i user secrets (inte i source control)
- **LocalDB** för utveckling, SQL Server för produktion


## 🧪 Test Data Seeding

Vid första körningen i Development-läge skapar `DataSeedHostingService` automatiskt:

### Kurser (6 st)

1. **C\# Fundamentals** - 4 moduler (Grundläggande syntax, OOP, Collections, Debugging)
2. **JavaScript Basics** - 4 moduler (JavaScript, DOM, Async, ES6)
3. **React Development** - 4 moduler (React grunder, Components, State, Routing)
4. **Python Basics** - 4 moduler (Syntax, Datastrukturer, Funktioner, API)
5. **ASP.NET Core** - 4 moduler (MVC, Entity Framework, API, Säkerhet)
6. **Fullstack .NET** - 4 moduler (C\#, ASP.NET API, Blazor, Deployment)

### Moduler

- **3-5 moduler** per kurs med realistiska namn
- **Tidsperioder** baserade på kursdatum
- **Beskrivningar** med svenska texter


### Aktiviteter

- **Föreläsningar** (2 timmar)
- **Övningar** (3 timmar)
- **Workshops** (4 timmar)
- **Projekt** (5-6 timmar)
- **Kunskapskontroller** (1 timme)


### Användare

- **6 lärare** med svenska namn (Julia Svensson, etc.)
- **30 studenter** med svenska namn
- **Automatisk fördelning**: 2 lärare per kurs, 5 studenter per kurs
- **Email format**: fornamn.efternamn@domain.com


## 📝 Databas Schema

### Huvudtabeller

**AspNetUsers** (Identity)

- Id, UserName, Email, PasswordHash
- FirstName, LastName (custom)
- RefreshToken, RefreshTokenExpireTime

**Courses**

- Id, Name, Description
- Starts, Ends (DateOnly)

**Modules**

- Id, Name, Description, CourseId
- Starts, Ends (DateOnly)

**ProjActivities**

- Id, Title, Description, Type, ModuleId
- Starts, Ends (DateTime)

**ProjDocuments**

- Id, DisplayName, FileName, Description
- UploadedByUserId, CourseId, ModuleId, ActivityId, StudentId
- IsSubmission, Status (Ej bedömd/Godkänd/Underkänd/Granskning)
- UploadedAt

**CourseUsers** (Many-to-Many)

- Id, UserId, CourseId, IsTeacher


### Relationer

- Course → Modules (1:n)
- Module → ProjActivities (1:n)
- ProjActivity → ProjDocuments (1:n)
- Course → CourseUsers (1:n)
- ApplicationUser → CourseUsers (1:n)


## 🐛 Felsökning

### Problem: Databas anslutning misslyckas

**Lösning**: Kontrollera att LocalDB är installerat och kör:

```bash
sqllocaldb info
```


### Problem: Migrations fel

**Lösning**: Ta bort databasen och kör om:

```bash
dotnet ef database drop
dotnet ef database update
```


### Problem: 401 Unauthorized i API-anrop

**Lösning**:

1. Kontrollera att JWT secret key är minst 32 tecken
2. Verifiera att token inte har expirerat
3. Kolla att `Authorization: Bearer {token}` header finns

### Problem: Notifieringar dekrypteras inte

**Lösning**:

1. Verifiera att `EncryptionKey` är **identisk** i båda user secrets
2. Nyckel måste vara minst 16 tecken
3. Starta om båda applikationerna efter ändring

### Problem: Port redan används

**Lösning**: Ändra port i `launchSettings.json`:

```json
"applicationUrl": "https://localhost:7213;http://localhost:5166"
```


## 🤝 Gruppmedlemmar

Detta projekt utvecklades av **Lexicon Fullstack Grupp 2** som en del av utbildningen **Systemutvecklare .NET** på Lexicon Yrkeshögskola.

### Bidragsgivare

<table>
  <tr>
    <td align="center">
      <img src="https://avatars.githubusercontent.com/u/105447315?v=4" width="100px" alt=""/><br />
      <b>OlenaKut</b><br />
      <i>Developer</i><br />
      <a href="https://github.com/OlenaKut">GitHub</a>
    </td>
    <td align="center">
      <img src="https://avatars.githubusercontent.com/u/204795899?v=4" width="100px" alt=""/><br />
      <b>NiMatts</b><br />
      <i>Developer</i><br />
      <a href="https://github.com/NiMatts">GitHub</a>
    </td>
  </tr>
</table>





## 📄 Licens

Detta är ett utbildningsprojekt utvecklat på Lexicon Yrkeshögskola. 
Detta projekt är licensierat under MIT License.


## 🙏 Acknowledgments

- **Lexicon Yrkeshögskola** - Utbildning och support
- **Microsoft** - .NET och Blazor framework
- **Bootstrap** - UI-komponenter
- **Bogus** - Testdata generering

---



