#!/usr/bin/env bash
set -e
CONN="${TECHSTORE_CONN:-Server=localhost;Database=TechStoreDB;Trusted_Connection=True;TrustServerCertificate=True;}"
dotnet ef dbcontext scaffold "$CONN" Microsoft.EntityFrameworkCore.SqlServer \
  --project TechStore.EFCore.DbFirst --startup-project TechStore.EFCore.DbFirst \
  --output-dir Models --context-dir Data --context TechStoreDbFirstContext --namespace TechStore.EFCore.DbFirst.Models \
  --no-onconfiguring --data-annotations --force
