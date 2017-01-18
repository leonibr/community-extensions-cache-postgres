-- FUNCTION: public.getcacheitemformat(text, text, text, timestamp with time zone)

-- DROP FUNCTION public.getcacheitemformat(text, text, text, timestamp with time zone);

CREATE OR REPLACE FUNCTION public.getcacheitemformat(
	"SchemaName" text,
	"TableName" text,
	"DistCacheId" text,
	"UtcNow" timestamp with time zone)
    RETURNS SETOF "TABLE(distcache_id text, distcache_value bytea, distcache_expiresattime timestamp with time zone, distcache_slidingexpirationinseconds bigint, distcache_absoluteexpiration timestamp with time zone)"
    LANGUAGE 'plpgsql'
    COST 100.0
    VOLATILE NOT LEAKPROOF 
    ROWS 1000.0
AS $function$

DECLARE v_Query Text;
DECLARE	var_r record;
        
BEGIN

v_Query := format('SELECT "Id", "Value", "ExpiresAtTime", "SlidingExpirationInSeconds", "AbsoluteExpiration" ' ||
                  'FROM %I.%I WHERE "Id" = $1 AND $2 <= "ExpiresAtTime"', "SchemaName", "TableName");

FOR var_r IN EXECUTE v_Query USING "DistCacheId", "UtcNow"
     LOOP
              	DistCache_Id := var_r."Id" ; 
       	      	DistCache_Value := var_r."Value";
        		DistCache_ExpiresAtTime := var_r."ExpiresAtTime";
        		DistCache_SlidingExpirationInSeconds := var_r."SlidingExpirationInSeconds";
        		DistCache_AbsoluteExpiration := var_r."AbsoluteExpiration";
              RETURN NEXT;
            END LOOP;
END

$function$;

ALTER FUNCTION public.getcacheitemformat(text, text, text, timestamp with time zone)
    OWNER TO postgres;
