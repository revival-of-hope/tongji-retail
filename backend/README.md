# Backend

ASP.NET Core 9 Minimal API + Entity Framework Core 9 + Oracle 21c XE.

## Docker build

The backend Docker build runs restore, xUnit tests, and publish in Linux. Host
`bin/` and `obj/` directories are recursively excluded and are also removed
inside the image before the final restore. This prevents Windows-generated
`project.assets.json` files from referencing Visual Studio fallback package
folders inside the Linux container.

```bash
docker compose build --no-cache --progress=plain backend
docker compose up
```

Runtime OpenAPI document:

```text
http://localhost:8080/openapi/v1.json
```

## Oracle 21c SQL compatibility

The application explicitly configures `OracleSQLCompatibility.DatabaseVersion21`.
This is required because the Oracle EF Core 23.x provider otherwise assumes Oracle
23 SQL semantics and may generate native `TRUE`/`FALSE` literals that Oracle 21c
cannot parse.
