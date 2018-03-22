-- FUNCTION: public.deletecacheitemformat(text, text, text)

-- DROP FUNCTION public.deletecacheitemformat(text, text, text);

CREATE OR REPLACE FUNCTION [schemaName].deletecacheitemformat(
	"SchemaName" text,
	"TableName" text,
	"DistCacheId" text)
    RETURNS void
    LANGUAGE 'plpgsql'
    COST 100.0
    VOLATILE NOT LEAKPROOF 
AS $function$

    
DECLARE v_Query Text;
BEGIN
 
 v_Query := format('DELETE FROM %I.%I WHERE "Id" = $1', "SchemaName", "TableName");
 EXECUTE v_Query using "DistCacheId";
    
END

$function$;

