-- PROCEDURE: public.deleteexpiredcacheitemsformat(text, text, timestamp with time zone)

-- DROP PROCEDURE public.deleteexpiredcacheitemsformat(text, text, timestamp with time zone);

CREATE OR REPLACE PROCEDURE [schemaName].deleteexpiredcacheitemsformat(
	"SchemaName" text,
	"TableName" text,
	"UtcNow" timestamp with time zone)
    LANGUAGE 'plpgsql'
AS $$

DECLARE v_Query Text;
DECLARE v_RowCount INT;
BEGIN

v_Query := format('DELETE FROM %I.%I WHERE $1 > "ExpiresAtTime"', "SchemaName", "TableName");
EXECUTE v_Query USING "UtcNow";
GET DIAGNOSTICS v_RowCount := ROW_COUNT;
RAISE NOTICE '[schemaName].deleteexpiredcacheitemsformat - DELETED - % entreis', v_RowCount;
    
END

$$;

