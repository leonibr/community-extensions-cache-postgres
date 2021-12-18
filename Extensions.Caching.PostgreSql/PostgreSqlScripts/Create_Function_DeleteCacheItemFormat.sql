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
DECLARE v_Hit bool;
DECLARE	var_r record;
BEGIN
 v_Hit := false;
RAISE NOTICE 'Executing [schemaName].deletecacheitemformat with parameters: SchemaName: "%", TableName: "%", DistCacheId: "%"', "SchemaName","TableName","DistCacheId";
 v_Query := format('DELETE FROM %I.%I WHERE "Id" = $1 RETURNING "Id"', "SchemaName", "TableName");
 FOR var_r IN EXECUTE v_Query USING "DistCacheId"
        LOOP
                v_Hit := true;
                RAISE NOTICE '[schemaName].deletecacheitemformat - DELETED - DistCacheId: "%" ',var_r."Id";
        END LOOP;
if v_Hit = false then
    RAISE NOTICE '[schemaName].deletecacheitemformat - MISSED - DistCacheId: "%", Item not found',var_r."Id";
end if;
END

$function$;

