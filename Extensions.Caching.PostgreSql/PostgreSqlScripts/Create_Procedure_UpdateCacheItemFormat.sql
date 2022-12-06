-- PROCEDURE: public.updatecacheitemformat(text, text, text, timestamp with time zone)

-- DROP PROCEDURE public.updatecacheitemformat(text, text, text, timestamp with time zone);

CREATE OR REPLACE PROCEDURE [schemaName].updatecacheitemformat(
	"SchemaName" text,
	"TableName" text,
	"DistCacheId" text,
	"UtcNow" timestamp with time zone)
    LANGUAGE 'plpgsql'
AS $$

DECLARE v_Query Text;
BEGIN

v_Query := format('UPDATE %I.%I ' ||
				  'SET "ExpiresAtTime" = '
                  	'CASE ' ||
                  		'WHEN (SELECT %I.datediff(''seconds''::varchar, $1, "AbsoluteExpiration")) <= "SlidingExpirationInSeconds" ' ||
                  		'THEN "AbsoluteExpiration" ' ||
         		  		'ELSE $1 + "SlidingExpirationInSeconds" *  interval ''1 second'' ' ||
    			  	'END ' ||
				  'WHERE "Id" = $2 ' ||
  				  'AND $1 <= "ExpiresAtTime" ' ||
  				  'AND "SlidingExpirationInSeconds" IS NOT NULL ' ||
  				  'AND ("AbsoluteExpiration" IS NULL OR "AbsoluteExpiration" <> "ExpiresAtTime")', "SchemaName", "TableName", "SchemaName");
EXECUTE v_Query using "UtcNow", "DistCacheId";   
RAISE NOTICE '[schemaName].updatecacheitemformat UPDATED entry for Id: %', "DistCacheId";
END
$$;

