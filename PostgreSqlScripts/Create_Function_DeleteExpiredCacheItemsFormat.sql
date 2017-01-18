-- FUNCTION: public.deleteexpiredcacheitemsformat(text, text, timestamp with time zone)

-- DROP FUNCTION public.deleteexpiredcacheitemsformat(text, text, timestamp with time zone);

CREATE OR REPLACE FUNCTION public.deleteexpiredcacheitemsformat(
	"SchemaName" text,
	"TableName" text,
	"UtcNow" timestamp with time zone)
    RETURNS void
    LANGUAGE 'plpgsql'
    COST 100.0
    VOLATILE NOT LEAKPROOF 
AS $function$

DECLARE v_Query Text;

BEGIN

v_Query := format('DELETE FROM %I.%I WHERE $1 > "ExpiresAtTime"', "SchemaName", "TableName");
EXECUTE v_Query USING "UtcNow";

END

$function$;

ALTER FUNCTION public.deleteexpiredcacheitemsformat(text, text, timestamp with time zone)
    OWNER TO postgres;
