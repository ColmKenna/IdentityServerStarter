dotnet ef migrations add  InitialIdentityServerPersistedGrantDbMigration  -p ..\IdentityServer.EF.DataAccess\IdentityServer.EF.DataAccess.csproj -c Duende.IdentityServer.EntityFramework.DbContexts.PersistedGrantDbContext -o ..\IdentityServer.EF.DataAccess\DataMigrations\Migrations\IdentityServer\PersistedGrantDb
dotnet ef migrations add InitialIdentityServerConfigurationDbMigration -p ..\IdentityServer.EF.DataAccess\IdentityServer.EF.DataAccess.csproj -c Duende.IdentityServer.EntityFramework.DbContexts.ConfigurationDbContext -o ..\IdentityServer.EF.DataAccess\DataMigrations/Migrations/IdentityServer/ConfigurationDb
dotnet ef migrations add Users -p ..\IdentityServer.EF.DataAccess\IdentityServer.EF.DataAccess.csproj -c IdentityServer.EF.DataAccess.DataMigrations.ApplicationDbContext -o ..\IdentityServer.EF.DataAccess\DataMigrations/Migrations/IdentityServer/Users



dotnet ef migrations add Update_DuendeIdentityServer_v7_0 -p ..\IdentityServer.EF.DataAccess\IdentityServer.EF.DataAccess.csproj -c Duende.IdentityServer.EntityFramework.DbContexts.ConfigurationDbContext -o ..\IdentityServer.EF.DataAccess\DataMigrations/Migrations/IdentityServer/ConfigurationDb


dotnet ef migrations add Update_DuendeIdentityServer_v7_0  -p ..\IdentityServer.EF.DataAccess\IdentityServer.EF.DataAccess.csproj -c Duende.IdentityServer.EntityFramework.DbContexts.PersistedGrantDbContext -o ..\IdentityServer.EF.DataAccess\DataMigrations\Migrations\IdentityServer\PersistedGrantDb

 -c PersistedGrantDbContext -o Migrations/PersistedGrantDb
 

