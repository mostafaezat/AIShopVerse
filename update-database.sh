#!/bin/bash

dotnet ef database update \
    --project Infrastructure \
    --startup-project AIShopVerse.EndUser/AIShopVerse.EndUser.Server \
    --context ApplicationDbContext
