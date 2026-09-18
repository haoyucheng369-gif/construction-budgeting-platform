#!/bin/bash
set -euo pipefail
export SQLCMDPASSWORD="$BUDGET_DB_PASSWORD"
exec /opt/mssql-tools18/bin/sqlcmd -S sqlserver -U budget_reader -d Budget -C -b -i /init/verify-readonly.sql
