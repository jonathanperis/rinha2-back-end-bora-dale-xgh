### Install tools:
```
dotnet tool install --global dotnet-ef
```

### Add migration (from root project folder)
```
dotnet ef migrations add InitialMigration -p ./src/Infrastructure -s ./src/WebAPI -c 'ApplicationDbContext'
```

### Apply migration (from root project folder)
```
dotnet ef database update -p ./src/Infrastructure -s ./src/WebAPI -c 'ApplicationDbContext'
```

### Remove migration (from root project folder)
```
dotnet ef migrations remove -p ./src/Infrastructure -s ./src/WebAPI -c 'ApplicationDbContext'
```