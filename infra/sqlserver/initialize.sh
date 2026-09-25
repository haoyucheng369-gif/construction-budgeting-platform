#!/bin/bash
set -euo pipefail

# 自动生成的密码不包含 SQL 分隔符；拒绝包含不支持字符的自定义密码。
if [[ ! "$BUDGET_DB_PASSWORD" =~ ^[a-zA-Z0-9_!@#%+=.-]{12,128}$ ]]; then
  echo 'BUDGET_DB_PASSWORD must be 12-128 characters using letters, digits or _!@#%+=.-' >&2
  exit 1
fi

/opt/mssql-tools18/bin/sqlcmd -S sqlserver -U sa -C -b \
  -v BudgetReaderPassword="$BUDGET_DB_PASSWORD" -i /init/budget.sql
