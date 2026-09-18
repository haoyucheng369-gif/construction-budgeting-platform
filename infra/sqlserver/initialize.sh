#!/bin/bash
set -euo pipefail

# Generated passwords contain no SQL delimiters. Reject unsupported custom values.
if [[ ! "$BUDGET_DB_PASSWORD" =~ ^[a-zA-Z0-9_!@#%+=.-]{12,128}$ ]]; then
  echo 'BUDGET_DB_PASSWORD must be 12-128 characters using letters, digits or _!@#%+=.-' >&2
  exit 1
fi

/opt/mssql-tools18/bin/sqlcmd -S sqlserver -U sa -C -b \
  -v BudgetReaderPassword="$BUDGET_DB_PASSWORD" -i /init/budget.sql
