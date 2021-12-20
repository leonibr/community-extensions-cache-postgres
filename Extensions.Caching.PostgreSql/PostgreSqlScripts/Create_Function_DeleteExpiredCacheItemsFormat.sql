-- FUNCTION: public.deleteexpiredcacheitemsformat(text, text, timestamp with time zone)

-- DROP FUNCTION public.deleteexpiredcacheitemsformat(text, text, timestamp with time zone);

CREATE OR REPLACE FUNCTION [schemaName].deleteexpiredcacheitemsformat(
	"SchemaName" text,
	"TableName" text,
	"UtcNow" timestamp with time zone)
    RETURNS void
    LANGUAGE 'plpgsql'
    COST 100.0
    VOLATILE NOT LEAKPROOF 
AS $function$

DECLARE v_Query Text;
DECLARE v_RowCount INT;
BEGIN

v_Query := format('DELETE FROM %I.%I WHERE $1 > "ExpiresAtTime"', "SchemaName", "TableName");
EXECUTE v_Query USING "UtcNow";
GET DIAGNOSTICS v_RowCount := ROW_COUNT;
RAISE NOTICE '[schemaName].deleteexpiredcacheitemsformat - DELETED - % entreis', v_RowCount;
    
END

$function$;

