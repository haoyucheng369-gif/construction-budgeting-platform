#!/bin/sh
set -eu

psql --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" --set ON_ERROR_STOP=1 \
  --set sales_password="$SALES_DB_PASSWORD" \
  --set library_password="$LIBRARY_DB_PASSWORD" \
  --set compositions_password="$COMPOSITIONS_DB_PASSWORD" \
  --set budget_password="$BUDGET_PROJECTION_DB_PASSWORD" <<'SQL'
REVOKE ALL ON DATABASE vente FROM PUBLIC;
REVOKE CREATE ON SCHEMA public FROM PUBLIC;

SELECT format('CREATE ROLE sales_app LOGIN PASSWORD %L', :'sales_password')
WHERE NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'sales_app') \gexec
SELECT format('CREATE ROLE library_app LOGIN PASSWORD %L', :'library_password')
WHERE NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'library_app') \gexec
SELECT format('CREATE ROLE compositions_app LOGIN PASSWORD %L', :'compositions_password')
WHERE NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'compositions_app') \gexec
SELECT format('CREATE ROLE budget_projection_app LOGIN PASSWORD %L', :'budget_password')
WHERE NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'budget_projection_app') \gexec

CREATE SCHEMA IF NOT EXISTS sales AUTHORIZATION sales_app;
CREATE SCHEMA IF NOT EXISTS library AUTHORIZATION library_app;
CREATE SCHEMA IF NOT EXISTS compositions AUTHORIZATION compositions_app;
CREATE SCHEMA IF NOT EXISTS budget_projection AUTHORIZATION budget_projection_app;
GRANT CONNECT ON DATABASE vente TO sales_app, library_app, compositions_app, budget_projection_app;
SQL
