\set ON_ERROR_STOP on

DO $$
BEGIN
    IF current_user <> 'sales_app' THEN
        RAISE EXCEPTION 'Verification must use the Sales service account';
    END IF;
    IF NOT has_schema_privilege(current_user, 'sales', 'CREATE') THEN
        RAISE EXCEPTION 'Sales cannot create objects in its own schema';
    END IF;
    IF has_schema_privilege(current_user, 'library', 'USAGE')
       OR has_schema_privilege(current_user, 'compositions', 'USAGE')
       OR has_schema_privilege(current_user, 'budget_projection', 'USAGE') THEN
        RAISE EXCEPTION 'Sales must not access another service schema';
    END IF;
END $$;

BEGIN;
CREATE TABLE sales.__infrastructure_check (id integer PRIMARY KEY);
INSERT INTO sales.__infrastructure_check VALUES (1);
SELECT id FROM sales.__infrastructure_check;
ROLLBACK;
