@echo off
where psql >nul 2>nul
if %ERRORLEVEL% neq 0 (
	echo Error: PostgreSQL command line tool 'psql' not found.
	echo Please ensure PostgreSQL Client is installed and added to your PATH.
	exit /b 1
)

set arg1=%1
if "%arg1%"=="-erase" (
	psql -U postgres -f erase-sample-database.sql
) 
if "%arg1%"=="-create" (
	echo 1^. Create database 'cache_test' into localhost
	echo using default postgres user
	echo 2^. Create a role also called 'cache_test'
	echo 3^. Connects to database and create a schema 'name1'
	psql -U postgres -f create-sample-database.sql
)
echo Done.



