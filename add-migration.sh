#!/bin/bash
MIGRATION_NAME=${1:-InitialCreate}

dotnet ef migrations add "$MIGRATION_NAME" \
    --project Infrastructure \
    --startup-project AIShopVerse.EndUser/AIShopVerse.EndUser.Server \
    --output-dir Persistence/Migrations \
    --context ApplicationDbContext
