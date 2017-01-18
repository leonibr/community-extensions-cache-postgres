-- FUNCTION: public.setcache(text, text, text, bytea, double precision, timestamp with time zone, timestamp with time zone)

-- DROP FUNCTION public.setcache(text, text, text, bytea, double precision, timestamp with time zone, timestamp with time zone);

CREATE OR REPLACE FUNCTION public.setcache(
	"SchemaName" text,
	"TableName" text,
	"DistCacheId" text,
	"DistCacheValue" bytea,
	"DistCacheSlidingExpirationInSeconds" double precision,
	"DistCacheAbsoluteExpiration" timestamp with time zone,
	"UtcNow" timestamp with time zone)
    RETURNS void
    LANGUAGE 'plpgsql'
    COST 100.0
    VOLATILE NOT LEAKPROOF 
AS $function$

DECLARE v_ExpiresAtTime TIMESTAMP(6) WITH TIME ZONE;
DECLARE v_RowCount INT;
DECLARE v_Query Text;
BEGIN

 CASE 
	 WHEN ("DistCacheSlidingExpirationInSeconds" IS NUll)
     THEN  v_ExpiresAtTime := "DistCacheAbsoluteExpiration"; 
     ELSE v_ExpiresAtTime := "UtcNow" + "DistCacheSlidingExpirationInSeconds" * interval '1 second';
 END CASE;
 
 v_Query := format('UPDATE %I.%I SET "Value" = $1, "ExpiresAtTime" = $2, "SlidingExpirationInSeconds" = $3, "AbsoluteExpiration" = $4 WHERE "Id" = $5', "SchemaName", "TableName");
 EXECUTE v_Query using "DistCacheValue", v_ExpiresAtTime, "DistCacheSlidingExpirationInSeconds", "DistCacheAbsoluteExpiration", "DistCacheId";
 
 GET DIAGNOSTICS v_RowCount := ROW_COUNT;

 IF(v_RowCount = 0) THEN INSERT INTO public."DistCache" ("Id", "Value", "ExpiresAtTime", "SlidingExpirationInSeconds", "AbsoluteExpiration") VALUES("DistCacheId", "DistCacheValue", v_ExpiresAtTime, "DistCacheSlidingExpirationInSeconds", "DistCacheAbsoluteExpiration"); END IF;
END

$function$;

ALTER FUNCTION public.setcache(text, text, text, bytea, double precision, timestamp with time zone, timestamp with time zone)
    OWNER TO postgres;
